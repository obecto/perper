
import pandas as pd
import gym
from gym import spaces
import numpy as np
from Environments.position import Position
from Data.random_load import random_train_load
from numpy import load
import torch
import gc

class StopLossMarket(gym.Env):
    def __init__(self,
                 env_config = {},
                 starting_money = 1000,
                 episode_length = 10000,
                ):
        self.tick = 0 # Start from the first tick
        self.episode_length = episode_length
        self.action_space = spaces.Discrete(2) # buy at some percentages / not_buy 
        self.high = np.array([500] * 100 + [2000])
        self.limit_high = np.append(self.high[:-1], [np.inf])
        self.low = np.array([0] * 101)
        self.observation_space = spaces.Box(high = self.limit_high, low = self.low, shape = (101,))
        #print("Version: {}  shape: {}   Actions: {}".format(self.version, low.shape, self.action_space))
        self.n_states = 100
        self.last_states = np.zeros(self.n_states)
        self.starting_money = starting_money # starting money = the money the agent starts with
        self.last_action = None
        
        # variables that track the current values
        self.current_money = self.starting_money
        self.comission_percent = 0.0025
        #self.icm = ICM(state_space = self.observation_space.shape[0] , action_space = self.action_space.n, representation_size = 128)
        self.current_price = 0
        self.positions = []
      
    def reset(self, ):
        gc.collect()
        #start from a random tick
        self.last_states = np.zeros(self.n_states)
        self.current_money = self.starting_money
        self.last_action = None
        self.current_price = 0
        self.data = random_train_load('Data/Label_based_RL/rl_data.csv', ep_size = self.episode_length)
        self.tick = 0
        self.positions = []
        self.current_price = self.data.iloc[self.tick]['prices']
        state = self.write_to_state_history(self.current_price)
        state = np.append(state, [self.current_money])
        state = self.normalize_state(state)
        return state
    
    def test(self, ):
        gc.collect()
        self.data = data_test
        self.tick = 0
        state = self.data.iloc[self.tick].to_numpy()
        self.current_price = state[0]
        state = np.append(state, [self.current_money])
        
        return state      
    def normalize_state(self, state):
        state = (state - self.low)/(self.high - self.low)
        return state
    def step(self, action):
        gain = self.check_positions()
        reward = gain                  
        self.current_money = self.current_money + gain

        if action == 0 or self.current_money == 0:
            #do nothing
            pass
        elif action < 2 or action > 0:  # do nothing
        # buy with 1, 2 or 5 percent of money
            coefficient_dict = {1:1}
            coefficient = coefficient_dict[action]
            money_to_buy_with = coefficient * self.current_money
            self.current_money = self.current_money - money_to_buy_with
            reward = reward - money_to_buy_with
            effective_buying_power = money_to_buy_with * (1 - self.comission_percent)
            position = Position(self.current_price, effective_buying_power)
            self.positions.append(position)
        else:
            raise('Action must be between 0 and 1')
        self.tick += 1
        state = self.data.iloc[self.tick].to_numpy()
        self.current_price = state[1]
        #state = np.append(state, [self.current_money])
        if self.tick == 9999:
            selling_all_gain = self.sell_all_positions()
            reward = reward + gain
            self.current_money += gain
        state = self.write_to_state_history(self.current_price)
        state = np.append(state, [self.current_money])
        state = self.normalize_state(state)
        return state, reward, 0, {}

    def write_to_state_history(self, price):
        place_holder = self.last_states[:-1]
        self.last_states = np.append([price], place_holder)
        return self.last_states 
    
    def sell_all_positions(self,):
        positions_to_close = []
        gain = 0
        for i in range(len(self.positions)):
                gain += self.positions[i].sell(self.current_price) * (1 - self.comission_percent)
                positions_to_close.append(i)
        positions_to_close = positions_to_close[::-1]
        for i in range(len(positions_to_close)):
            del self.positions[positions_to_close[i]]
        return gain
    
    def check_positions(self,):
        #check and close ready positions
        positions_to_close = []
        gain = 0
        for i in range(len(self.positions)):
            ripe = self.positions[i].step(self.current_price)
            if ripe == 1:
                gain += self.positions[i].sell(self.current_price) * (1 - self.comission_percent)
                positions_to_close.append(i)
        positions_to_close = positions_to_close[::-1]
        for i in range(len(positions_to_close)):
            del self.positions[positions_to_close[i]]
        return gain