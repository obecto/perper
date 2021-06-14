import asyncio
import json
import gc

import pandas as pd
import gym
from gym import spaces
import matplotlib.pyplot as plt
import numpy as np
from numpy import load
import torch
from ray.rllib.env import ExternalEnv

import perper
import perper.jupyter as jupyter
from perper.model import Stream

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
        print(f"Running... ")
        while True:
            action = self.get_action(episode_id, obs)
            obs, reward, done, info = self.env.step(action)
            self.log_returns(episode_id, reward, info=info)
            if done:
                self.end_episode(episode_id, obs)
                obs = self.reset()
                episode_id = self.start_episode()
                
    def reset(self):
        print(f"Reset External env wrapper")
        obs = self.event_loop.run_until_complete(self.env.reset())
#         self.env.reset()
        return obs
                
class DataLoader():
    def __init__(self, episode_length, data_path):
        self.episode_length = episode_length
        self.stream_name = self.get_stream_name()
        self.stream = Stream(streamname = self.stream_name)
        self.stream.set_parameters(jupyter.ignite, jupyter.fabric, instance=jupyter.instance, serializer=jupyter.serializer, state=None)
        
        self.column_names = None
        self.generator_got = False
        
        self.high = load(data_path + 'high.npy')
        self.low = load(data_path + 'low.npy')
        
    def get_stream_name(self):
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
        
    def get_high_low(self):
        return self.high, self.low
        
    async def get_column_names(self):
        item = await self.async_gen.__anext__()
        col_names = json.loads(item.Json)["Columns"]
        self.column_names = pd.Series(col_names.split(','))
        
    async def get_generator(self):
        self.generator_got = True
        self.async_gen = await self.stream.get_async_generator()
        print("Generator created")
        
    async def get_episode(self):
        if not self.generator_got:
            print(63)
            await self.get_generator()
        if type(self.column_names) != pd.core.series.Series:
            print(64)
            await self.get_column_names()
        episode_data = []
        for i in range(self.episode_length):
            item = await self.async_gen.__anext__()
            row = np.fromstring(json.loads(item.Json)["Row"], dtype=float, sep=',')
            episode_data.append(row)
            
        episode_data = np.array(episode_data)
        episode_data = pd.DataFrame(data= episode_data, columns= self.column_names)
        print("Get episode end")
        return episode_data

class MarketEnv():
    def __init__(self, env_config ):
        self.starting_money = env_config['starting_money']
        self.starting_stocks = env_config['starting_stocks']
        self.episode_length = env_config['episode_length']
        self.commission = env_config['commission']
        self.curiosity_scale = env_config['curiosity_reward']
        data_path = env_config['data_path']
        
        self.loader = DataLoader(self.episode_length, data_path)
            
        self.high, self.low = self.loader.get_high_low()
        self.env_config = env_config
        
        #Change high low to be env specific
        self.modify_high_low()
        
        self.observation_space = spaces.Box(high = np.inf, low = -np.inf, shape = (len(self.high),))
        self.set_action_space()
        
    def modify_high_low(self,):
        pass
    
    def set_action_space(self,):
        return NotImplemented
    
    def modify_state(self, state):
        return state
    
    def extract_action_info(self, action):
        return NotImplemented
    
    def do_action(self, action_info):
        return NotImplemented
    
    async def reset(self,):
        self.current_money = self.starting_money
        self.current_stocks = self.starting_stocks
        self.tick = 0
        
        self.data = await self.loader.get_episode()
        self.episode_length = len(self.data)
        
        state = self.data.iloc[self.tick] 
        self.current_price = state['price']
        self.potential_initial_stock = self.starting_money/self.current_price + self.starting_stocks
        state = state.values
        state = self.modify_state(state)
        return state.copy()
    
    def test(self,):
        self.current_money = self.starting_money
        self.current_stocks = self.starting_stocks
        self.tick = 0
        
        self.data = self.loader.get_test()
        self.episode_length = len(self.data)
        
        state = self.data.iloc[self.tick]     
        self.current_price = state['price']
        self.potential_initial_stock = self.starting_money/self.current_price + self.starting_stocks
        state = state.values
        
        # Change state to be env specific
        state = self.modify_state(state)
        
        return state.copy()
    
    def sell(self, amount):
        if amount > self.current_stocks:
            self.punish()
        else:
            stock_delta = -amount
            cash_delta = amount * self.current_price * (1 - self.commission)
            self.current_stocks += stock_delta
            self.current_money += cash_delta
            
        trade_info = [stock_delta, cash_delta]
            
        return trade_info
    
    def buy(self, amount):
        if amount > self.current_money:
            self.punish()
        else:
            stock_delta = amount * (1-self.commission)/self.current_price
            cash_delta = -amount
            self.current_stocks += stock_delta
            self.current_money += cash_delta
            
        trade_info = [stock_delta, cash_delta]
            
        return trade_info
    
    def normalize_state(self, state):
        normalized_state = (state - self.low)/(self.high - self.low + 1e-17)
        
        return normalized_state
    
    def calculate_market_beater(self):
        market_delta = (self.potential_initial_stock * self.current_price-self.starting_money)/self.starting_money
        asset_delta =  (self.current_money + self.current_stocks * self.current_price - self.starting_money)/self.starting_money
        return asset_delta - market_delta
        
    def step(self, action):
        action_info = self.extract_action_info(action)
        return self.do_action(action_info)

    