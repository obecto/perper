#!/usr/bin/env python
# coding: utf-8

# In[1]:


import pandas as pd
import numpy as np
from numpy import load


# In[2]:


from Data.random_load import random_train_load


# In[3]:


class EpisodeLoader():
    def __init__(self, data_path, episode_length):
        self.data_path = data_path
        data_path.split('/')
        self.data_name = data_path.split('/')[-2] + '.csv'
        self.high = load(data_path + 'high.npy')
        self.low = load(data_path + 'low.npy')

        self.episode_length = episode_length
        
    def get_episode(self,):
        self.data, self.candles = random_train_load(self.data_path + self.data_name, ep_size = self.episode_length)
        return self.data
    
    def get_high_low(self,):
        return self.high, self.low
    
    def get_test(self,):
        return pd.read_csv(self.data_path + 'test.csv')

