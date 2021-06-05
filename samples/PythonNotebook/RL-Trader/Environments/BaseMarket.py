import pandas as pd
import gym
import matplotlib.pyplot as plt
from gym import spaces
import numpy as np
from numpy import load
import torch
import gc


from Data.random_load import random_train_load
from Data.episode_load import EpisodeLoader



class MarketEnv(gym.Env):
    def __init__(self, env_config ):
        self.data = env_config['data']
        self.starting_money = env_config['starting_money']
        self.starting_stocks = env_config['starting_stocks']
        self.episode_length = env_config['episode_length']
        self.commission = env_config['commission']
        self.curiosity_scale = env_config['curiosity_reward']
        self.loader = EpisodeLoader(self.data, episode_length = self.episode_length)
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
    
    def reset(self,):
        self.current_money = self.starting_money
        self.current_stocks = self.starting_stocks
        self.tick = 0
        
        self.data = self.loader.get_episode()
        self.episode_length = len(self.data)
        
        state = self.data.iloc[self.tick]     
        self.current_price = state['price']
        self.potential_initial_stock = self.starting_money/self.current_price + self.starting_stocks
        state = state.values
        
        # Change state to be env specific
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
        normalized_state = (state - self.low)/(self.high - self.low)
        
        return normalized_state
    
    def calculate_market_beater(self):
        market_delta = (self.potential_initial_stock * self.current_price-self.starting_money)/self.starting_money
        asset_delta =  (self.current_money + self.current_stocks * self.current_price - self.starting_money)/self.starting_money
        return asset_delta - market_delta
        
    def step(self, action):
        action_info = self.extract_action_info(action)
        return self.do_action(action_info)