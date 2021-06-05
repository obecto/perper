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
from Data.episode_load import EpisodeLoader
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


def add_noise(state, noise_percentage = 0.23, bit_noise = 0.1):
    categorical_var = state[:-9]
    continuous_var = state[-9:-1]
    flawed_continuous = continuous_var + noise_percentage * np.random.randn(len(continuous_var))
    categorical_noise = bit_noise * abs(np.random.randn(*categorical_var.shape))
    ones = np.where(categorical_var == 1 )[0]
    zeros = np.where(categorical_var == 0)[0]
    categorical_var[ones] = 1 - categorical_noise[ones]
    categorical_var[zeros] = 0 + categorical_noise[zeros]
    flipped_categorical = categorical_var
    flawed_state = np.append(flipped_categorical, flawed_continuous)
    flawed_state = np.append(flawed_state, [state[-1]])
    return flawed_state




class SimpleMarket(gym.Env):
    def __init__(self, env_config = {
                               'last_states_num': None,
                               'curiosity_reward':150,
                               'continuous':False,
                               'data': 'Data/ground_truth/'
                                },
                 starting_money = 1000,
                 episode_length = 10000,
                 comission = 0.0025,
                 noise_to_continuous = 0,
                 chance_to_flip = 0.0
                ):
        #noise parameters
        self.noise_to_continuous = noise_to_continuous
        self.chance_to_flip = chance_to_flip
        
        #dataset = 'Data/predictions/'
        dataset = env_config['data']
        
        self.loader = EpisodeLoader(dataset, episode_length = episode_length)
        self.high = self.loader.get_high_low()[0] 
        self.low = self.loader.get_high_low()[1]
        
        self.high = np.append(self.high, [100., 2000., 1000., self.high[-1]])
        
        self.low = np.append(self.low, [0., 0., 0., self.low[-1]])
        
        self.env_config = env_config
        self.tick = 0 # Start from the first tick
        self.episode_length = episode_length
        if self.env_config['continuous'] == False:
            self.action_space = spaces.Discrete(7) # buy/sell 10/50/100% + hold
        else:
            self.action_space = spaces.Box(high = 1, low = -1, shape = (1,))
        self.curiosities = np.array([])
        self.observation_space = spaces.Box(high = np.inf, low = -np.inf, shape = (len(self.high),))
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
        
        self.data = self.loader.get_episode()
        
        self.tick = 0 
        state = self.data.iloc[self.tick]     
        self.current_price = state['price']
        #state = np.append(state[1:], [100, self.current_money, 0])
        #CHANGE NEXT DUMMY ACTION TO ZERO
        state = np.append(state, [100, self.current_money, 0, self.zero_line])
        #print(state, len(state))
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
        
        self.data = self.loader.get_test()
        self.tick = 0 
        state = self.data.iloc[self.tick]
        self.current_price = self.data["price"][self.tick]
        #state = np.append(state[1:], [100, self.current_money, 0])
        #CHANGE NEXT DUMMY ACTION TO ZERO
        state = np.append(state, [100, self.current_money, 0, self.zero_line])
        return state.copy()       
        
    def normalize_state(self, state):
        normalized_state = (state - self.low)/(self.high - self.low)
        
        return normalized_state

    def step(self, action):
        #assert self.action_space.contains(action)
        assert self.assets > 0
        done = 0
        reward = 0
        hold_reward = 0
        stock_delta = 0
        transaction = 0 
#       Initiating local variables

        state = self.data.iloc[self.tick].to_numpy()
        self.current_price = self.data["price"][self.tick]
        
            
        self.assets = self.current_money + self.current_stock * self.current_price
        percent_in_cash = 100 * self.current_money/self.assets
        state = np.append(state, [percent_in_cash, self.current_money, self.assets-self.current_money, self.zero_line])
        previous_stock = self.current_stock
        
        
#       Insert evaluation of reward for different action spaces here
            
#       Determining reward and state for complex action space  

        if self.env_config['continuous'] == False:
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
            transaction_percentage = action_dict[action]  
        else:
            
            if action > 0:
                action_type = 'buy'
                if self.current_money == 0:
                    useless_action = 1
                    action_type = 'hold'
            elif action == 0:
                action_type = 'hold'
            elif action < 0:
                action_type = 'sell'
                if self.current_stock == 0:
                    useless_action = 0
                    action_type = 'hold'
            transaction_percentage = abs(action[0])
#           Updating current money and stock according to action

#           When holding get the same reward as when selling a part of the currently owned stock
        if action_type == 'hold':
            hold_reward = self.hold_coeff * self.current_stock * (self.current_price - self.zero_line)
            #reward += hold_reward

        if action_type == 'buy':    

#               Buy stock for buy_coeff of current cash

            buy_coeff = transaction_percentage
            transaction = buy_coeff * self.current_money
            comission = self.comission_percent * transaction                  
            stock_delta = transaction/self.current_price
            reward -= comission 
            self.zero_line = (self.zero_line * self.current_stock + stock_delta*self.current_price)/(self.current_stock + stock_delta)
            self.current_money -= (transaction + comission)
            self.current_stock += stock_delta

        if action_type == 'sell':

#               Sell stock for sell_coeff of current stock

            sell_coeff = transaction_percentage
            transaction = sell_coeff * self.current_stock * self.current_price 
            comission = self.comission_percent * transaction
            stock_delta = -transaction/self.current_price
            reward = (self.current_price-self.zero_line)*-stock_delta - comission
            self.current_money += -stock_delta*self.current_price - comission
            
            self.current_stock += stock_delta

        if action == 6 or self.current_stock == 0:
            self.zero_line = 0
        logger.debug("Action: {}   Reward: {:.3f}    Zero line:{:.3f}   Price:{:.3f}".format(action, reward, self.zero_line, self.current_price)) 

#           Load next state and calculate tracking values
        self.tick += 1
        self.assets = self.current_money + self.current_stock * self.current_price
        percent_in_cash = 100 * self.current_money/self.assets
        self.asset_delta += self.assets - self.last_assets
        self.last_assets = self.assets
        next_state = self.data.iloc[self.tick].to_numpy()
        next_state = np.append(next_state, [percent_in_cash, self.current_money, self.assets-self.current_money, self.zero_line])

#           Check for the end of the file
        if self.tick == len(self.data)-1:
            done = 1

#           Tracking the overall movement of the reward
        self.reward_delta += reward
    
        #Add curiosity bonuses
        self.current_price = self.data["price"][self.tick]
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
                                            'transaction_size': transaction,
                                            'action_type':action_type,
                                            'current_price':self.current_price,
                                            'stock_monetized': self.current_stock * self.current_price,
                                            'current_money': self.current_money,
                                            'hold_reward':hold_reward,
                                            'reward': reward,
                                            'assets': self.assets,
                                            'total_stocks': self.current_stock,
                                            'previous_stock': previous_stock,}
                                             #'no_curiosity_reward': no_curiosity_reward}

