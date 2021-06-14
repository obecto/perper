#!/usr/bin/env python
# coding: utf-8

import pandas as pd
import gym
import matplotlib.pyplot as plt
from gym import spaces
import numpy as np
from numpy import load
import gc
import logging
from Environments.curiosity import ICM, RND
from Data.episode_load import EpisodeLoader

path = 'Data/predictions/'
loader = EpisodeLoader(path, 10000)
data_test = loader.get_test()

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

class Trade():
    def __init__(self, buy_price, amount, timestep, coeff=0.05):
        self.buy_price = buy_price
        self.ripe = 0
        self.lower_bound = (1-coeff)*self.buy_price
        self.upper_bound = (1+coeff)*self.buy_price
        self.amount = amount
        self.age = 0
        self.adulthood_age = 100
        self.timestep = timestep
        
    def update_state(self, price):
        if self.upper_bound <= price or price <= self.lower_bound:
            self.ripe = 1
        elif self.age>self.adulthood_age:
            self.ripe = 1
        else:
            self.ripe = 0
        self.age += 1
        
    def is_ripe(self, ):
        return self.ripe

class Env_ray(gym.Env):
    def __init__(self, env_config = {'action_mode': 1,
                               'last_states_num': None,
                               'curiosity_reward':1
                                },
                 starting_money = 1000,
                 episode_length = 1000
                ):
        self.env_config = env_config
        self.tick = 0 # Start from the first tick
        
        self.state_trades_num = 10
        self.set_low_high()
                    
        if self.env_config['action_mode'] == 1:
            self.action_space = spaces.Discrete(4) # buy/sell 10/50/100% + hold 
        elif self.env_config['action_mode'] == 2:
            self.action_space = spaces.Tuple((spaces.Discrete(4), spaces.Box(low=0.5, high=1.0, shape=(1,))))
        
        self.observation_space = spaces.Box(high = np.inf, low = -np.inf, shape = self.low.shape)
        logger.info("Shape: {}   Actions: {}".format(self.low.shape, self.action_space))
        
        self.starting_money = starting_money # starting money = the money the agent starts with
        self.last_action = None
        
        # variables that track the current values
        self.reward_delta = 0
        self.asset_delta = 0
        self.current_money = self.starting_money 
        self.current_stock = 0
        self.last_assets = 0
        self.last_cost = 0
        self.comission_percent = 0.0025
        
        self.trades = []    
        self.state_trades = []
        self.curiosities = np.array([])
        
        #ICM
#         self.icm = ICM(state_space = self.observation_space.shape(), action_space = self.action_space.shape(), representation_size = 128)
        self.rnd = RND(state_space = self.observation_space.shape[0], hidden_size = 256, representation_size = 128)
        
    def reset(self, ):
        gc.collect()
        #start from a random tick
        
        self.data = loader.get_episode()
        self.tick = 0
        self.current_money = self.starting_money
        self.current_stock = 0
        
        self.trades = []    
        self.state_trades = []
        self.curiosities = np.array([])
        
        state = self.data.iloc[self.tick].to_numpy()
        self.potential_initial_stock = self.current_money/self.data["price"][self.tick]
            
        state = np.append(state, [100, self.current_money, 0])
        self.update_state_trades()
        state = np.append(state, self.state_trades)
        logger.info('-'*30 + "Reset called" + '-'*30)
        
        return state.copy()
    
    def test(self, ):
        gc.collect()
        self.data = data_test
        self.tick = 0
        self.current_money = self.starting_money
        self.current_stock = 0
        
        self.trades = []    
        self.state_trades = []
        self.curiosities = np.array([])
        
        state = self.data.iloc[self.tick].to_numpy()
        self.potential_initial_stock = self.current_money/self.data["price"][self.tick]
    
        state = np.append(state, [100, self.current_money, 0])
        self.update_state_trades()
        state = np.append(state, self.state_trades)
        logger.info('-'*30 + "Test called" + '-'*30)
        
        return state.copy()
    
    def step(self, action):
        assert self.action_space.contains(action)
                
#       Initiating local variables  
        done = 0
        reward = 0
        percent_in_cash = 100
        state = self.data.iloc[self.tick].to_numpy()
        current_price = self.data["price"][self.tick]            
            
        assets = self.current_money + self.current_stock * current_price 
        percent_in_cash = 100 * self.current_money/assets
        state = np.append(state, [percent_in_cash, self.current_money, assets-self.current_money])
        self.update_state_trades()
        state = np.append(state, self.state_trades)
        
#       Insert evaluation of reward for different action spaces here
                
#       Determining reward and state for complex action space        

        self.update_trade_states(current_price)

        buy_coeffs = {0:0.01,
                      1:0.02,
                      2:0.05
        }
        transaction = 0
        previous_stock = 0
        buy_rewards_info = []
        
        if self.env_config['action_mode'] == 1:
#           Determining action type
            if action in [0, 1, 2]:
                action_type = 'buy'
                buy_coeff = buy_coeffs[action]
            elif action == 3:
                action_type = 'sell'
               
            logger.debug("Before the action \n Action_type: {}    Current_money: {:.3f}    Current_stock: {:.3f}    Current_assets: {:.3f}    Reward: {:.3f}".format(action_type, self.current_money, self.current_stock*current_price, assets, reward))
                                
            if action_type == 'buy':
                transaction = self.current_money * buy_coeff
                self.current_money -= transaction
                amount = transaction/current_price
                self.current_stock += amount 
                trade = Trade(current_price, amount, self.tick)
                self.trades.append(trade)
            elif action_type == 'sell':
                buy_rewards_info = self.calculate_buy_rewards(current_price)
                reward = self.sell_ripe(current_price)
        
        if self.env_config['action_mode'] == 2:
#           Determining action type
            if action[0] in [0, 1, 2]:
                action_type = 'buy'
                buy_coeff = buy_coeffs[action[0]]
            elif action[0] == 3:
                action_type = 'sell'
            box_coeff = action[1]
               
            logger.debug("Before the action \n Action_type: {}    Current_money: {:.3f}    Current_stock: {:.3f}    Current_assets: {:.3f}    Reward: {:.3f}".format(action_type, self.current_money, self.current_stock*current_price, assets, reward))
                                
            if action_type == 'buy':
                transaction = self.current_money * buy_coeff
                self.current_money -= transaction
                amount = transaction/current_price
                self.current_stock += amount 
                trade = Trade(current_price, amount, self.tick, box_coeff)
                self.trades.append(trade)
            elif action_type == 'sell':
                buy_rewards_info = self.calculate_buy_rewards(current_price)
                reward = self.sell_ripe(current_price)
            
#       Load next state and calculate tracking values
        self.tick += 1
        assets = self.current_money + self.current_stock * current_price
        percent_in_cash = 100 * self.current_money/assets
        self.asset_delta += assets - self.last_assets
        self.last_assets = assets

        next_state = self.data.iloc[self.tick].to_numpy()

#       Additional values for the state
        next_state = np.append(next_state, [percent_in_cash, self.current_money, assets-self.current_money])

#       Updating trades to inlcude in state
        self.update_state_trades()
        next_state = np.append(next_state, self.state_trades)
      
        #Add curiosity bonuses
        n_state = self.normalize_state(state)
        n_next_state = self.normalize_state(next_state)
        
#       Check for the end of the file
        if self.tick == len(self.data)-1:
            done = 1

        curiosity = float(self.rnd.train_step(n_next_state).detach())
        self.curiosities = np.append(self.curiosities, curiosity)
#             no_curiosity_reward = reward
#             reward += self.env_config['curiosity_reward'] * curiosity

        market_beater = self.calculate_market_beater(current_price)
        
        logger.debug("After the action \n Action_type: {}    Current_money: {:.3f}    Current_stock: {:.3f}    Current_assets: {:.3f}    Reward: {:.3f} \n".format(action_type, self.current_money, self.current_stock*current_price, assets, reward))

        info = {'action':action,
                'transaction_size': transaction,
                'action_type':action_type,
                'current_price':current_price,
                'stock_monetized': self.current_stock * current_price,
                'current_money': self.current_money,
                'reward': reward,
                'assets': assets,
                'total_stocks': self.current_stock,
                'previous_stock': previous_stock,
                'no_curiosity_reward': 0, #no_curiosity_reward,
                'buy_rewards_info': buy_rewards_info,
                'market_beater':market_beater}

        
        return n_next_state.copy(), reward, done, info
    
    def set_low_high(self):
        self.low, self.high = loader.get_high_low()
            
        self.high = np.append(self.high, [101, 2000, 1000])
        self.low = np.append(self.low, [-1, -1, -1])
            
#       We add boundary values for a number of trades to keep track off
#       We add two slots for all the trades left, ripe unripe respectively      

        state_trades_lows = [-1]*(self.state_trades_num+2)
        state_trades_highs = [np.inf]*(self.state_trades_num+2)
            
        self.low = np.append(self.low, state_trades_lows)
        self.high = np.append(self.high, state_trades_highs)
        
    
    def trade_delta(self, trade, price):
        delta = trade.amount*(price - trade.buy_price)  

        return delta
    
    def sell_ripe(self, sell_price):        
        reward = 0
        for trade in self.trades:
            if trade.is_ripe():
                reward += self.trade_delta(trade, sell_price)
                self.current_money += trade.amount*sell_price
                self.current_stock -= trade.amount
                self.trades.remove(trade)
        
        return reward
    
    def calculate_buy_rewards(self, sell_price):
        buy_rewards_info = []
        for trade in self.trades:
            if trade.is_ripe():
                buy_reward = self.trade_delta(trade, sell_price)
                buy_rewards_info.append([trade.timestep, buy_reward])
        return buy_rewards_info
    
    def update_trade_states(self, price):      
        for trade in self.trades:
            trade.update_state(price)
            
    def total_stock_amount(self):
        stock_amount = 0
        for trade in self.trades:
            stock_amount += trade.amount
            
        return stock_amount    
    
    def normalize_state(self, state):
        normalized_state = (state - self.low)/(self.high - self.low)
        return normalized_state
    
#   When there are not enough trades made yet fill the left slots with zeros
    def extend_with_zeros(self):
        assert self.state_trades_num >= len(self.trades)
        zeros_extension = np.zeros(self.state_trades_num - len(self.trades)+2)
        self.state_trades = np.append(self.state_trades, zeros_extension)

    def fill_main_slots(self):
        assert self.state_trades_num < len(self.trades)
        for trade in self.trades[::-1][:self.state_trades_num]:
            self.state_trades.append(trade.amount*trade.buy_price)
    
    def update_state_trades(self, mode='volume'):    
            
#       Calculate the sums for the trades outside the main slots
        def get_ripe_unripe_sums(trades_left):
            ripe = []
            unripe = []
            for trade in trades_left:
                if trade.is_ripe():
                    ripe.append(trade.amount*trade.buy_price)
                else:
                    unripe.append(trade.amount*trade.buy_price)
                    
            if not ripe:
                ripe_sum = 0
            else:
                ripe_sum = sum(ripe)
                
            if not unripe:
                unripe_sum = 0
            else:
                unripe_sum = sum(unripe)
            
            return ripe_sum, unripe_sum
        
#       Fill the main slots with respect to mode and then fill the two slots remaining
        self.state_trades = []
        trades_copy = self.trades
        
        if mode == 'time':
            pass
                
        if mode == 'volume':
            trades_copy.sort(key=lambda x:x.amount*x.buy_price)
            
#       If there are not enough trades made yet extend with zeroes
        if len(trades_copy) <= self.state_trades_num:
            for trade in trades_copy[::-1]:
                self.state_trades = np.append(self.state_trades, trade.amount*trade.buy_price)
            self.extend_with_zeros()
            
#       Fill main slots and calculate means for the two other slots
        else:
            self.fill_main_slots()
            trades_left = trades_copy[:-self.state_trades_num]
            ripe_sum, unripe_sum = get_ripe_unripe_sums(trades_left)
            self.state_trades = np.append(self.state_trades, [ripe_sum, unripe_sum])


        if len(self.state_trades) != self.state_trades_num + 2:
            print("The trades to include in the state have an incorrect format")
            
    def calculate_market_beater(self, current_price):
        market_delta = (self.potential_initial_stock*current_price-self.starting_money)/self.starting_money
        asset_delta =  (self.current_money + self.current_stock * current_price - self.starting_money)/self.starting_money
        return asset_delta - market_delta
                        
    
            