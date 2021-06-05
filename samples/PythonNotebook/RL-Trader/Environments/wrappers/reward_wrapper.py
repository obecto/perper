#!/usr/bin/env python
# coding: utf-8

# In[1]:


import gym
from ray.rllib.utils.filter import MeanStdFilter
import numpy as np
from Environments.curiosity import RND
# In[2]:


class RewardWrapper(gym.Wrapper):
    def __init__(self, env):
        super().__init__(env)
        self.env = env
        self.shape = self.env.observation_space.shape
        self.mean_std_track = MeanStdFilter(())
    def step(self, action):
        next_state, reward, done, info = self.env.step(action)
        
        self.mean_std_track(reward)
        mean = self.mean_std_track.rs.mean
        std = self.mean_std_track.rs.std
        reward = (reward - mean)/(std + 1e-15)
        return next_state, reward, done, info


class CuriosityWrapper(gym.Wrapper):
    def __init__(self, env):
        super().__init__(env)
        self.env = env
        self.shape = self.env.observation_space.shape
        self.mean_std_track = MeanStdFilter(())
        self.curiosity_mean_std = MeanStdFilter(())
        self.rnd = RND(state_space = env.observation_space.shape[0], hidden_size = 256, representation_size = 128)
        self.scale = self.env.env_config['curiosity_reward']
        
    def step(self, action):
        next_state, reward, done, info = self.env.step(action)
        curiosity = float(self.rnd.train_step(next_state).detach())
        self.curiosity_mean_std(curiosity)
        c_mean = self.curiosity_mean_std.rs.mean
        c_std = self.curiosity_mean_std.rs.std
        
        self.mean_std_track(reward)
        mean = self.mean_std_track.rs.mean
        std = self.mean_std_track.rs.std
        extrinsic_reward = (reward - mean)/(std + 1e-15)
        intrinsic_reward = (curiosity - c_mean)/(c_std + 1e-15)
        reward = extrinsic_reward + self.scale * intrinsic_reward
        return next_state, reward, done, info