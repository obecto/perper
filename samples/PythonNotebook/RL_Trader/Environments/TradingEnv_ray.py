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
#from curiosity import ICM
from Environments.curiosity import RND
from Data.random_load import random_train_load
from Environments.position import Position
data_test = pd.read_csv('Data/test_set.csv')
data_test = data_test.iloc[:, 1:-36]

class Env_ray(gym.Env):
    def __init__(self, env_config = {'action_mode': 1,
                               'last_states_num': None,
                               'version': 'long',
                               'curiosity_reward':150,
                                },
                 starting_money = 1000,
                 episode_length = 1000
                ):
        self.env_config = env_config
        self.tick = 0 # Start from the first tick
        self.version = env_config['version']
        
        if self.version == 'long':
            low = load('Data/low_values.npy')
            high = load('Data/high_values.npy')

        if self.version == 'short':
            low = load('Data/low_values.npy')[np.array([0, -3, -2, -1])]
            high = load('Data/high_values.npy')[np.array([0, -3, -2, -1])]
          
        if self.env_config['action_mode'] == 1:
            self.action_space = spaces.Discrete(7) # buy/sell 10/50/100% + hold 
        self.low = low
        self.high = high
        self.curiosities = np.array([])
        self.observation_space = spaces.Box(high = self.high, low = self.low, shape = low.shape)
        #print("Version: {}  shape: {}   Actions: {}".format(self.version, low.shape, self.action_space))
        
        self.starting_money = starting_money # starting money = the money the agent starts with
        self.last_action = None
        self.hold_coeff = 0.1
        
        # variables that track the current values
        self.reward_delta = 0
        self.asset_delta = 0
        self.current_money = self.starting_money
        self.current_stock = 0
        self.zero_line = 0   
        self.last_assets = 0
        self.last_cost = 0
        self.comission_percent = 0.0025
        #self.icm = ICM(state_space = self.observation_space.shape[0] , action_space = self.action_space.n, representation_size = 128)
        self.rnd = RND(state_space = self.observation_space.shape[0], hidden_size = 256, representation_size = 128)
    def reset(self, ):
        gc.collect()
        #start from a random tick
        
        self.data = random_train_load('Data/indicator_dataset.csv')
        self.tick = 0 
        if self.version == 'short':
            self.data = self.data.iloc[:, 1]     
            state = self.data.iloc[self.tick]
        else:
            self.data = self.data.iloc[:, 1:-36]
            state = self.data.iloc[self.tick].to_numpy()
            
        state = np.append(state, [100, self.current_money, 0])

        return state.copy()
    
    def test(self, ):
        gc.collect()
        self.data = data_test
        self.tick = len(self.data) -  7*24*60
        
        if self.version == 'short':
            state = self.data.iloc[self.tick]
        else:
            state = self.data.iloc[self.tick].to_numpy()
            
        state = np.append(state, [100, self.current_money, 0])
        
        return state.copy()       
        
    def normalize_state(self, state):
        normalized_state = (state - self.low)/(self.high - self.low)
        return normalized_state
    
    def step(self, action):
        assert self.action_space.contains(action)
        done = 0
        reward = 0
        hold_reward = 0
        stock_delta = 0
        
             
#       Initiating local variables
        if self.version == 'short':
            state = self.data.iloc[self.tick]
            current_price = self.data[self.tick]
            #print(state, current_price)
        else:
            state = self.data.iloc[self.tick].to_numpy()
            current_price = self.data["price"][self.tick]
            
            
        assets = self.current_money + self.current_stock * current_price 
        percent_in_cash = 100 * self.current_money/assets
        state = np.append(state, [percent_in_cash, self.current_money, assets-self.current_money])
        
        
        
#       Insert evaluation of reward for different action spaces here
            
#       Determining reward and state for complex action space  

        if self.env_config['action_mode'] == 1:
            action_dict = {0:0,
                           1:0.01,
                           2:0.02,
                           3:0.05,
                           4:0.01,
                           5:0.02,
                           6:0.05
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
                reward += hold_reward
            
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
                     
#           print("Action: {}   Reward: {:.3f}    Zero line:{:.3f}   Price:{:.3f}".format(action, reward, self.zero_line, current_price)) 

#           Load next state and calculate tracking values
            self.tick += 1
            assets = self.current_money + self.current_stock * current_price
            percent_in_cash = 100 * self.current_money/assets
            self.asset_delta += assets - self.last_assets
            self.last_assets = assets
            if self.version == 'short':
                next_state = self.data.iloc[self.tick]
            else:
                next_state = self.data.iloc[self.tick].to_numpy()
            next_state = np.append(next_state, [percent_in_cash, self.current_money, assets-self.current_money])
            
#           Check for the end of the file
            if self.tick == len(self.data)-1:
                done = 1
            
#           Tracking the overall movement of the reward
            self.reward_delta += reward 
        #Add curiosity bonuses
        n_state = self.normalize_state(state)
        n_next_state = self.normalize_state(next_state)
        
        curiosity = float(self.rnd.train_step(n_next_state).detach())
        self.curiosities = np.append(self.curiosities, curiosity)
        no_curiosity_reward = reward
        reward += self.env_config['curiosity_reward'] * curiosity
        #print(self.env_config['curiosity_reward'] * curiosity, no_curiosity_reward)
        return next_state.copy(), reward, done, {'action':action,
                                            'stock_size':stock_delta,
                                            'useless_action': useless_action,
                                            'transaction_size': transaction,
                                            'action_type':action_type,
                                            'current_price':current_price,
                                            'stock_monetized': self.current_stock * current_price,
                                            'current_money': self.current_money,
                                            'hold_reward':hold_reward,
                                            'reward': reward,
                                            'assets': assets,
                                            'total_stocks': self.current_stock,
                                            'previous_stock': previous_stock,
                                             'no_curiosity_reward': no_curiosity_reward}

class StopLossMarket(gym.Env):
    def __init__(self, env_config = {'action_mode': 1,
                               'last_states_num': None,
                               'version': 'long',
                               'curiosity_reward':150,
                                },
                 starting_money = 1000,
                 episode_length = 1000
                ):
        self.env_config = env_config
        self.tick = 0 # Start from the first tick
        self.version = env_config['version']
        
          
        if self.env_config['action_mode'] == 1:
            self.action_space = spaces.Discrete(2) # buy/not_buy 
        self.low = low
        self.high = high
        self.curiosities = np.array([])
        self.observation_space = spaces.Box(high = np.inf, low = -np.inf, shape = (15,))
        #print("Version: {}  shape: {}   Actions: {}".format(self.version, low.shape, self.action_space))
        
        self.starting_money = starting_money # starting money = the money the agent starts with
        self.last_action = None
        
        # variables that track the current values
        self.current_money = self.starting_money
        self.comission_percent = 0.0025
        #self.icm = ICM(state_space = self.observation_space.shape[0] , action_space = self.action_space.n, representation_size = 128)
        self.rnd = RND(state_space = self.observation_space.shape[0], hidden_size = 256, representation_size = 128)
        self.current_price = 0
        self.open_positions = []
      
    def reset(self, ):
        gc.collect()
        #start from a random tick
        
        self.data = random_train_load('Data/rl_data.csv')
        self.tick = 0
        self.current_price = self.data.iloc[tick]['price']
        state = self.data.iloc[self.tick].to_numpy()
        state = np.append(state, [self.current_money])
        return state.copy()
    
    def test(self, ):
        gc.collect()
        self.data = data_test
        self.tick = 0
        state = self.data.iloc[self.tick].to_numpy()
        self.current_price = state[0]
        state = np.append(state, [self.current_money])
        
        return state.copy()       
    
    def step(self, action):
        gain = self.check_positions()
        reward = gain                  
        self.current_money = self.current_money + gain
        if action == 0:
            #do nothing
            pass
        elif action < 4 or action > 0:  # do nothing
        # buy with 1, 2 or 5 percent of money
            coefficient_dict = {1:0.01, 2:0.02, 3:0.05}
            coefficient = coefficent_dict[action]
            money_to_buy_with = coefficient * self.current_money
            self.current_money = self.current_money - money_to_buy_with
            reward = reward - money_to_buy_with
            effective_buying_power = money_to_buy_with * (1 - self.comission_price)
            position = Position(self.current_price, effective_buying_power)
            self.positions.append(position)
        else:
            raise('Action must be between 0 and 3')
        self.tick += 1
        state = self.data.iloc[self.tick].to_numpy()
        self.current_price = state[0]
        state = np.append(state, [self.current_money])
        return state, reward, 0, {}

    def check_positions(self,):
        #check and close ready positions
        positions_to_close = []
        gain = 0
        for i in range(len(self.positions)):
            ripe = self.positions[i].step(self.current_price)
            if ripe == 1:
                gain += self.positions[i].sell(self.current_price) * (1 - self.comission_price)
                positions_to_close.append(i)
        for i in range(len(positions_to_close)):
            del self.positions[positions_to_close[i]]
        return gain
       

class SineMarketEnv(gym.Env):
    #One trader one sine-moving asset trading env
    def __init__(self,  env_config = {'last_states_num': None}, spread = 0.0025, speed = 0.25, noise = 0.1, starting_money = 1000, starting_stock = 0, starting_price = 500, scale = 100, input_step = 0.1):
        # spread =  difference between the price at which you buy the asset and the one you can sell the asset at as a percentage of buy price
        self.spread = spread
        self.env_config = env_config
        # noise = noise on price generation as a percentage of the scale
        self.noise = noise
        # how fast does the price change
        self.speed = speed
        # starting money = the money the agent starts with
        self.starting_money = starting_money
        # starting stock = the stock the agent starts with
        self.starting_stock = starting_stock
        # starting price = the price at which the stock begins trade day
        self.starting_price = starting_price
        # scale = how scaled is the sine function Ex. with scale = 100 the price will range from starting_price-100 to starting_price + 100
        self.scale = scale
        # step = how much does the input of the sine function change at each step
        self.input_step = input_step
        # variables that track the current values
        self.timesteps = 0
        self.current_price = self.starting_price
        self.current_money = self.starting_money
        self.current_stock = self.starting_stock
        self.current_input = 0
        #cost of last bought actions
        self.last_cost = 0
        self.last_assets = self.starting_money + self.starting_stock * self.starting_price
        
        self.action_space = gym.spaces.Discrete(2)
        self.last_action = 0
        if self.env_config['last_states_num'] is not None:
            state = np.array([self.current_price, self.current_money, self.current_stock, self.last_action])
            self.last_states = np.zeros(4 * self.env_config['last_states_num'])
            self.last_states = np.concatenate((self.last_states[4:], state))
            low = np.tile(np.array([0,-np.inf,-10,0]),env_config['last_states_num'])
            high = np.tile(np.array([1800,np.inf,np.inf,1]),env_config['last_states_num'])
            self.observation_space = gym.spaces.Box(low , high, shape = (4 * self.env_config['last_states_num'],))
        else:
            self.observation_space = gym.spaces.Box(low = np.array([0,-np.inf,-10,0]),
                                                    high = np.array([1800,np.inf,np.inf,1]), shape = (4 ,))

    
    def reset(self,):
        # reset the market with the same settings
        self.current_price = self.starting_price
        self.current_money = self.starting_money
        self.current_stock = self.starting_stock
        self.current_input = 0
        self.last_action = 0
        self.timesteps = 0
        self.last_cost = 0
        state = np.array([self.current_price, self.current_money, self.current_stock, self.last_action])
        if self.env_config['last_states_num'] is not None:
            state = self.last_states
        return state
    
    def step(self, action ):
        done = 0
        reward = 0
        #action {-1 : 1} short to long
        if action == 0 and self.last_action != 0:
            #sell if any
            money_from_transaction = self.current_stock *  (1 - self.spread) * self.current_price 
            self.current_money += money_from_transaction
            #calculate gain or loss from transaction
            reward = money_from_transaction - self.last_cost
            self.current_stock = 0
            self.last_action = 0
        if action == 1 and self.last_action != 1:
            #buy if any
            self.current_stock += self.current_money/self.current_price
            self.last_cost = self.current_money
            self.current_money = 0
            self.last_action = 1
            
        self.current_input += self.input_step
        # calculate next expected value 
        self.current_price = self.starting_price + self.scale * np.sin(self.speed * self.current_input)
        
        # gaussian noise in the price        
        self.current_price = np.random.normal(self.current_price, self.noise * self.scale)
           
        #make sure price is positive
        
        if self.current_price < 0:
            self.current_price = 1
            
        assets = self.current_money + self.current_stock * self.current_price
        self.asset_delta = assets - self.last_assets
        self.last_assets = assets
        #Rewards according to difference in net worth
        #reward = delta
        state = np.array([self.current_price, self.current_money, self.current_stock, self.last_action])
        self.timesteps = self.timesteps + 1
        #if self.timesteps == 1000:
        #    return state, reward, 1, {}
        if self.env_config['last_states_num'] is not None:
            self.last_states = np.concatenate((self.last_states[4:], state))
            state = self.last_states
       
        return state, reward, done, {}
