import asyncio
import json
import numpy as np

from ray.rllib.env import ExternalEnv
from gym import spaces
import torch

from perper.model import Stream
import perper.jupyter as jupyter

class TestEnv(ExternalEnv):
    def __init__(self, env):
        ExternalEnv.__init__(self, env.action_space, env.observation_space)
        self.env = env
        self.event_loop = asyncio.new_event_loop()
        
    def isAlive(self):
        return True
    
    def run(self):
        episode_id = self.start_episode()
        obs = self.reset()
        while True:
            print("Running...")
            action = self.get_action(episode_id, obs)
            print(f"The action: {action}")
            obs, reward, done, info = self.event_loop.run_until_complete(self.env.step(action))
            self.log_returns(episode_id, reward, info=info)
            if done:
                self.end_episode(episode_id, obs)
                obs = self.env.reset()
                episode_id = self.start_episode()
    
    def reset(self):
        obs = self.event_loop.run_until_complete(self.env.reset())
        return obs
        
class ConstantEnv():
    def __init__(self):
        self.action_space = spaces.Discrete(2)
        self.observation_space = spaces.Box(low=-1.0, high=2.0, shape=(1,), dtype=np.float32)
        self.counter = None
        stream_name = self.determine_stream_name()
        # TODO: Update stream implementation if needed! https://bit.ly/3wlJfmM; https://bit.ly/3guvhZ3
        self.stream = Stream(streamname = stream_name)
        self.stream.set_parameters(jupyter.ignite, jupyter.fabric, instance=jupyter.instance, serializer=jupyter.serializer, state=None)
        self.generator_set = False
    
    async def set_generator(self):
        self.generator_set = True
        self.async_gen = await self.stream.get_async_generator()
        print("Generator created")
        
    def determine_stream_name(self):
        stream_name = None

        for cache_name in jupyter.ignite.get_cache_names():
        #     if "generator" in cache_name and "blank" not in cache_name:
            if "-" in cache_name and "$" not in cache_name:
                stream_name = cache_name
                break

        if stream_name == None:
            raise Exception("No stream initiated yet")
        else:
            print("Stream name: " + stream_name)
            return stream_name
        
    async def reset(self):
        if not self.generator_set:
            await self.set_generator()
            
        self.counter = 0
        self.done = 0
        
        item = await self.async_gen.__anext__()
        val = int(item.Priority)%2
        val = np.array(val, dtype=float).copy()
        
        print(f"Reset called, value: {val}")
            
        return val
    
    async def step(self, action):
        if not self.generator_set:
            await self.set_generator()
            
        item = await self.async_gen.__anext__()
        val = int(item.Priority)%2
        val = np.array(val, dtype=float).copy()
        
        self.counter += 1
        if self.counter == 100:
            self.done = 1
            
        print(f"Step called, value: {type(val)}, {type(action)}")    
        return val, action, self.done, {}
    
