#!/usr/bin/env python
# coding: utf-8

# In[1]:


import pandas as pd
import gym
import matplotlib.pyplot as plt
from gym import spaces
import numpy as np
import gc

eps= 1

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
            self.observation_space = gym.spaces.Box(low = np.array([0,-np.inf,-10,0]), high = np.array([1800,np.inf,np.inf,1]), shape = (4 ,))
    #def _setup_spaces(self):
    #    self.action_space = gym.spaces.Discrete(2)
    #    self.observation_space = gym.spaces.Box(low = [-np.inf], high = [np.inf], shape = (4,))
    
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
        info = {}
        #action {-1 : 1} short to long
        if action == 0 and self.last_action != 0:
            #sell if any
            money_from_transaction = self.current_stock *  (1 - self.spread) * self.current_price 
            self.current_money += money_from_transaction
            #calculate gain or loss from transaction
            info = {'sold_price' : self.current_price, 'amount':self.current_stock,}
            reward = money_from_transaction - self.last_cost
            self.current_stock = 0
            self.last_action = 0
        if action == 1 and self.last_action != 1:
            #buy if any
            amount = self.current_money/self.current_price
            self.current_stock += amount
            self.last_cost = self.current_money
            self.current_money = 0
            self.last_action = 1
            info = {'bought_price' : self.current_price, 'amount':amount,}
        self.current_input += self.input_step
        # calculate next expected value 
        self.current_price = self.starting_price + self.scale * np.sin(self.speed * self.current_input)
        
        # gaussian noise in the price        
        self.current_price = np.random.normal(self.current_price, self.noise * self.scale)
           
        #make sure price is positive
        
        if self.current_price < 0:
            self.current_price = 1
            
        assets = self.current_money + self.current_stock * self.current_price
        delta = assets - self.last_assets
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
        return state, reward, done, info

