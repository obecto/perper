#!/usr/bin/env python
# coding: utf-8

# In[1]:


import logging
import time

import ray
from ray.rllib.agents.ppo import ppo
from ray.rllib.agents.trainer import with_base_config
from ray.rllib.execution.rollout_ops import ParallelRollouts
from ray.rllib.execution.metric_ops import StandardMetricsReporting
from ray.rllib.execution.common import STEPS_SAMPLED_COUNTER,     STEPS_TRAINED_COUNTER, LEARNER_INFO, LEARN_ON_BATCH_TIMER,     _get_shared_metrics
from ray.rllib.evaluation.rollout_worker import get_global_worker
from ray.rllib.utils.sgd import do_minibatch_sgd
from ray.rllib.agents.ppo.ddppo import DEFAULT_CONFIG, validate_config
logger = logging.getLogger(__name__)


# In[2]:


def execution_plan(workers, config):
    rollouts = ParallelRollouts(workers, mode="raw")

    # Setup the distributed processes.
    if not workers.remote_workers():
        raise ValueError("This optimizer requires >0 remote workers.")
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
        #print(batch)
        print(batch['infos'])
        expected_batch_size = (
            config["rollout_fragment_length"] * config["num_envs_per_worker"])
        this_worker = get_global_worker()
        assert batch.count == expected_batch_size,             ("Batch size possibly out of sync between workers, expected:",
             expected_batch_size, "got:", batch.count)
        logger.info("Executing distributed minibatch SGD "
                    "with epoch size {}, minibatch size {}".format(
                        batch.count, config["sgd_minibatch_size"]))
        info = do_minibatch_sgd(batch, this_worker.policy_map, this_worker,
                                config["num_sgd_iter"],
                                config["sgd_minibatch_size"], ["advantages"])
        return info, batch.count


# In[3]:


DDPPOTrainer = ppo.PPOTrainer.with_updates(
    name="DDPPO",
    default_config=DEFAULT_CONFIG,
    execution_plan=execution_plan,
    validate_config=validate_config)

