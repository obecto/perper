#!/usr/bin/env python
# coding: utf-8

import pandas as pd
import gym
import matplotlib.pyplot as plt
from gym import spaces
import numpy as np
from numpy import load
import torch
import gc
from Environments.curiosity import ICM
from Environments.curiosity import RND
from Data.random_load import random_train_load
import logging

# create logger
logger = logging.getLogger('Boxes envirnoment')
logger.setLevel(logging.DEBUG)

# create console handler and set level to debug
ch = logging.StreamHandler()
ch.setLevel(logging.INFO)

# create formatter
formatter = logging.Formatter('%(asctime)s - %(name)s - %(levelname)s - %(message)s')

# add formatter to ch
ch.setFormatter(formatter)

# add ch to logger
logger.addHandler(ch)


def add_noise(state, noise_percentage = 0.23, flip_chance = 0.23):
    categorical_var = state[:-9]
    continuous_var = state[-9:-1]
    flawed_continuous = continuous_var + noise_percentage * np.random.randn(len(continuous_var))
    bits_to_flip = np.where(np.random.uniform(size = (len(categorical_var),)) < flip_chance)[0]
    flipped_categorical = categorical_var
    flipped_categorical[bits_to_flip] = 1 - categorical_var[bits_to_flip]
    flawed_state = np.append(flipped_categorical, flawed_continuous)
    flawed_state = np.append(flawed_state, [state[-1]])
    return flawed_state

class SimpleMarket(gym.Env):
    def __init__(self, env_config = {'action_mode': 1,
                               'last_states_num': None,
                               'curiosity_reward':150,
                                },
                 starting_money = 1000,
                 episode_length = 10000,
                 comission = 0.0025,
                 noise_to_continuous = 0.25,
                 chance_to_flip = 0.25
                ):
        #noise parameters
        self.noise_to_continuous = noise_to_continuous
        self.chance_to_flip = chance_to_flip
        
        self.high = load('Data/Label_based_RL/high.npy')[1:]
        self.high = np.append(self.high, [100., 2000., 1000., self.high[-1]])
        
        self.low = load('Data/Label_based_RL/low.npy')[1:]
        self.low = np.append(self.low, [0., 0., 0., self.low[-1]])
        
        self.env_config = env_config
        self.tick = 0 # Start from the first tick
        self.episode_length = episode_length
        if self.env_config['action_mode'] == 1:
            self.action_space = spaces.Discrete(7) # buy/sell 10/50/100% + hold 
        self.curiosities = np.array([])
        self.observation_space = spaces.Box(high = np.inf, low = -np.inf, shape = (24,))
#         logger.debug("Version: {}  shape: {}   Actions: {}".format(self.version, low.shape, self.action_space))
        
        self.starting_money = starting_money # starting money = the money the agent starts with
        self.last_action = None
        self.hold_coeff = 0.1
        self.assets = 1000
        # variables that track the current values
        self.reward_delta = 0
        self.asset_delta = 0
        self.current_money = self.starting_money
        self.current_stock = 0
        self.zero_line = 0   
        self.last_assets = 0
        self.last_cost = 0
        self.comission_percent = comission
        #self.icm = ICM(state_space = self.observation_space.shape[0] , action_space = self.action_space.n, representation_size = 128)
        self.rnd = RND(state_space = self.observation_space.shape[0], hidden_size = 256, representation_size = 128)
        self.current_price = 0
    def reset(self, ):
        gc.collect()
        #start from a random tick
        self.current_price = 0
        self.zero_line = 0 
        self.last_assets = 0
        self.last_cost = 0
        self.reward_delta = 0
        self.asset_delta = 0
        self.current_money = self.starting_money
        self.assets = self.starting_money
        self.current_stock = 0
        
        self.data = random_train_load('Data/Label_based_RL/new_labels.csv', ep_size = self.episode_length)
        self.tick = 0 
        state = self.data.iloc[self.tick]
        #state = np.append(state[1:], [100, self.current_money, 0])
        #CHANGE NEXT DUMMY ACTION TO ZERO
        state = np.append(state[1:], [100, self.current_money, 0, self.zero_line])
        #print(state, len(state))
        self.current_price = state[0]
        return state.copy()
    def test(self, ):
        gc.collect()
        #start from a random tick
        self.current_price = 0
        self.zero_line = 0 
        self.last_assets = 0
        self.last_cost = 0
        self.reward_delta = 0
        self.asset_delta = 0
        self.current_money = self.starting_money
        self.assets = self.starting_money
        self.current_stock = 0
        
        self.data = pd.read_csv('Data/Label_based_RL/last_ten.csv')
        self.tick = 0 
        state = self.data.iloc[self.tick]
        #state = np.append(state[1:], [100, self.current_money, 0])
        #CHANGE NEXT DUMMY ACTION TO ZERO
        state = np.append(state[1:], [100, self.current_money, 0, self.zero_line])
        self.current_price = state[0]
        return state.copy()       
        
    def normalize_state(self, state):
        normalized_state = (state - self.low)/(self.high - self.low)
        
        return normalized_state

    def step(self, action):
        assert self.action_space.contains(action)
        assert self.assets > 0
        done = 0
        reward = 0
        hold_reward = 0
        stock_delta = 0
        
             
#       Initiating local variables

        state = self.data.iloc[self.tick].to_numpy()
        current_price = self.data["price"][self.tick]
        
            
        self.assets = self.current_money + self.current_stock * current_price
        percent_in_cash = 100 * self.current_money/self.assets
        state = np.append(state[1:], [percent_in_cash, self.current_money, self.assets-self.current_money, self.zero_line])
        
        
        
#       Insert evaluation of reward for different action spaces here
            
#       Determining reward and state for complex action space  

        if self.env_config['action_mode'] == 1:
            action_dict = {0:0,
                           1:0.1,
                           2:0.2,
                           3:0.5,
                           4:0.1,
                           5:0.5,
                           6:1
            }
            transaction = 0
            previous_stock = None
            
#           Determining action type and checking for impossible actions

            useless_action = 0
            if action == 0:
                action_type = 'hold'
            
            if action in [1, 2, 3]:
                if self.current_money == 0:
                    useless_action = 1
                    action_type = 'hold'
                else:
                    action_type = 'buy'

            if action in [4, 5, 6]:
                if self.current_stock == 0:
                    useless_action = 1
                    action_type = 'hold'
                else:
                    action_type = 'sell'
                    
#           Updating current money and stock according to action

#           When holding get the same reward as when selling a part of the currently owned stock

            if action_type == 'hold':
                hold_reward = self.hold_coeff * self.current_stock * (current_price - self.zero_line)
                #reward += hold_reward
            
            if action_type == 'buy':    
                
#               Buy stock for buy_coeff of current cash

                buy_coeff = action_dict[action]
                transaction = buy_coeff * self.current_money
                comission = self.comission_percent * transaction                  
                stock_delta = transaction/current_price
                reward -= comission 
                self.zero_line = (self.zero_line * self.current_stock + stock_delta*current_price)/(self.current_stock + stock_delta)
                self.current_money -= (transaction + comission)
                self.current_stock += stock_delta
                
            if action_type == 'sell':
                
#               Sell stock for sell_coeff of current stock
                
                sell_coeff = action_dict[action]
                transaction = sell_coeff * self.current_stock * current_price 
                comission = self.comission_percent * transaction
                stock_delta = -transaction/current_price
                reward = (current_price-self.zero_line)*-stock_delta - comission
                self.current_money += -stock_delta*current_price - comission
                previous_stock = self.current_stock
                self.current_stock += stock_delta
            
            if action == 6:
                self.zero_line = 0
                     
            logger.debug("Action: {}   Reward: {:.3f}    Zero line:{:.3f}   Price:{:.3f}".format(action, reward, self.zero_line, current_price)) 

#           Load next state and calculate tracking values
            self.tick += 1
            self.assets = self.current_money + self.current_stock * current_price
            percent_in_cash = 100 * self.current_money/self.assets
            self.asset_delta += self.assets - self.last_assets
            self.last_assets = self.assets
            next_state = self.data.iloc[self.tick].to_numpy()
            next_state = np.append(next_state[1:], [percent_in_cash, self.current_money, self.assets-self.current_money, self.zero_line])
            
#           Check for the end of the file
            if self.tick == len(self.data)-1:
                done = 1
            
#           Tracking the overall movement of the reward
            self.reward_delta += reward 
        #Add curiosity bonuses
        self.current_price = next_state[-5]
        state = add_noise(state, self.noise_to_continuous, self.chance_to_flip)
        next_state = add_noise(next_state, self.noise_to_continuous, self.chance_to_flip)
        n_state = self.normalize_state(state)
        n_next_state = self.normalize_state(next_state)
        '''
        curiosity = float(self.rnd.train_step(n_next_state).detach())
        self.curiosities = np.append(self.curiosities, curiosity)
        no_curiosity_reward = reward
        reward += self.env_config['curiosity_reward'] * curiosity
        '''
#         logger.debug(self.env_config['curiosity_reward'] * curiosity, no_curiosity_reward)
        
        return n_next_state.copy(), reward, done, {'action':action,
                                            'stock_size':stock_delta,
                                            'useless_action': useless_action,
                                            'transaction_size': transaction,
                                            'action_type':action_type,
                                            'current_price':current_price,
                                            'stock_monetized': self.current_stock * current_price,
                                            'current_money': self.current_money,
                                            'hold_reward':hold_reward,
                                            'reward': reward,
                                            'assets': self.assets,
                                            'total_stocks': self.current_stock,
                                            'previous_stock': previous_stock,}
                                             #'no_curiosity_reward': no_curiosity_reward}

