from ray.rllib.agents.ppo.ddppo import DDPPOTrainer
from ray.rllib.execution.rollout_ops import ParallelRollouts
import ray
import logging
from ray.rllib.execution.metric_ops import StandardMetricsReporting
import time
from ray.rllib.evaluation.rollout_worker import get_global_worker
from ray.rllib.utils.sgd import do_minibatch_sgd
logger = logging.getLogger(__name__)
from ray.rllib.execution.common import STEPS_SAMPLED_COUNTER,     STEPS_TRAINED_COUNTER, LEARNER_INFO, LEARN_ON_BATCH_TIMER, _get_shared_metrics

def custom_execution_plan(workers, config):
    rollouts = ParallelRollouts(workers, mode="raw")

    # Setup the distributed processes.
    if not workers.remote_workers():
        raise ValueError("This optimizer requires > 0 remote workers.")
    ip = ray.get(workers.remote_workers()[0].get_node_ip.remote())
    port = ray.get(workers.remote_workers()[0].find_free_port.remote())
    address = "tcp://{ip}:{port}".format(ip=ip, port=port)
    logger.info("Creating torch process group with leader {}".format(address))

    # Get setup tasks in order to throw errors on failure.
    ray.get([
        worker.setup_torch_data_parallel.remote(
            address, i, len(workers.remote_workers()), backend="gloo")
        for i, worker in enumerate(workers.remote_workers())
    ])
    logger.info("Torch process group init completed")

    # This function is applied remotely on each rollout worker.
    def train_torch_distributed_allreduce(batch):
        buy_info = []
        for step in range(len(batch['infos'])):
            useless_action = batch['infos'][step]['useless_action']
            action_type = batch['infos'][step]['action_type']
            current_price = batch['infos'][step]['current_price']
            stock_size = batch['infos'][step]['stock_size']
            total_stocks_before_sell = batch['infos'][step]['previous_stock']
            total_stocks = batch['infos'][step]['total_stocks']
            current_money = batch['infos'][step]['current_money']
            if action_type == 'buy' and useless_action == 0:
                b_info = {'step': step,
                          'price': current_price,
                          'volume' : stock_size,
                          'contribution': 0,                 
                         }
                buy_info.append(b_info)
            if action_type == 'sell' and useless_action == 0:
                dead_buys = []
                for i in range(len(buy_info)):
                    contribution = (-stock_size * buy_info[i]['volume'] * (current_price - buy_info[i]['price']))
                    contribution = contribution/total_stocks_before_sell
                    buy_info[i]['contribution'] += contribution
                    batch['rewards'][buy_info[i]['step']] += contribution
                    buy_info[i]['volume'] = buy_info[i]['volume'] * (total_stocks/total_stocks_before_sell)
        
        expected_batch_size = (
            config["rollout_fragment_length"] * config["num_envs_per_worker"])
        this_worker = get_global_worker()
        assert batch.count == expected_batch_size, \
            ("Batch size possibly out of sync between workers, expected:",
             expected_batch_size, "got:", batch.count)
        logger.info("Executing distributed minibatch SGD "
                    "with epoch size {}, minibatch size {}".format(
                        batch.count, config["sgd_minibatch_size"]))
        info = do_minibatch_sgd(batch, this_worker.policy_map, this_worker,
                                config["num_sgd_iter"],
                                config["sgd_minibatch_size"], ["advantages"])
        return info, batch.count

    # Have to manually record stats since we are using "raw" rollouts mode.
    class RecordStats:
        def _on_fetch_start(self):
            self.fetch_start_time = time.perf_counter()

        def __call__(self, items):
            for item in items:
                info, count = item
                metrics = _get_shared_metrics()
                metrics.counters[STEPS_SAMPLED_COUNTER] += count
                metrics.counters[STEPS_TRAINED_COUNTER] += count
                metrics.info[LEARNER_INFO] = info
            # Since SGD happens remotely, the time delay between fetch and
            # completion is approximately the SGD step time.
            metrics.timers[LEARN_ON_BATCH_TIMER].push(time.perf_counter() -
                                                      self.fetch_start_time)

    train_op = (
        rollouts.for_each(train_torch_distributed_allreduce)  # allreduce
        .batch_across_shards()  # List[(grad_info, count)]
        .for_each(RecordStats()))

    # Sync down the weights. As with the sync up, this is not really
    # needed unless the user is reading the local weights.
    if config["keep_local_weights_in_sync"]:

        def download_weights(item):
            workers.local_worker().set_weights(
                ray.get(workers.remote_workers()[0].get_weights.remote()))
            return item

        train_op = train_op.for_each(download_weights)

    # In debug mode, check the allreduce successfully synced the weights.
    if logger.isEnabledFor(logging.DEBUG):

        def check_sync(item):
            weights = ray.get(
                [w.get_weights.remote() for w in workers.remote_workers()])
            sums = []
            for w in weights:
                acc = 0
                for p in w.values():
                    for k, v in p.items():
                        acc += v.sum()
                sums.append(float(acc))
            logger.debug("The worker weight sums are {}".format(sums))
            assert len(set(sums)) == 1, sums

        train_op = train_op.for_each(check_sync)
    return StandardMetricsReporting(train_op, workers, config)

C_DDPPOTrainer = DDPPOTrainer.with_updates(
    execution_plan=custom_execution_plan)