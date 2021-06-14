import asyncio
import json
import numpy as np

from ray.rllib.env import ExternalEnv
from gym import spaces
import torch

from perper.model import Stream
import perper.jupyter as jupyter
from RL_Trader.Environments.BaseMarket import DataLoader

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
            obs, reward, done, info = self.env.step(action)
            self.log_returns(episode_id, reward, info=info)
            if done:
                self.end_episode(episode_id, obs)
                obs = self.reset()
                episode_id = self.start_episode()
    
    def reset(self):
        obs = self.event_loop.run_until_complete(self.env.reset())
        return obs
        
class ConstantEnv():
    def __init__(self):
        self.episode_length = 100
        self.loader = DataLoader(episode_length = self.episode_length)
        self.high, self.low = self.loader.get_high_low()
        
        self.action_space = spaces.Discrete(2)
        self.observation_space = spaces.Box(high = np.inf, low = -np.inf, shape = (len(self.high),))
        
    async def reset(self):            
        self.tick = 0
        self.done = 0
        
        self.data = await self.loader.get_episode()
        self.episode_length = len(self.data)
        
        state = self.data.iloc[self.tick] 
        self.current_price = state['price']
        state = state.values
        
        print(f"Reset called, value: {type(state)}")
            
        return state
    
    def step(self, action):        
        self.tick += 1
        if self.tick == self.episode_length-1:
            self.done = 1
        
        reward = action
        next_state = self.data.iloc[self.tick].values
            
        print(f"Step called, value: {type(next_state)}, {type(reward)}")    
        return next_state, reward, self.done, {}
    
