#!/usr/bin/env python
# coding: utf-8

# In[1]:


from torch.nn import functional as F
import torch.nn as nn
import torch
import numpy as np


class ICM_network(nn.Module):
    def __init__(self, state_space, action_space, representation_size):
        super(ICM_network, self).__init__()
        self.icm_common = nn.Linear(state_space, representation_size)
        self.icm_forward = nn.Linear(representation_size + action_space, representation_size)
        self.icm_inverse = nn.Linear(2 * representation_size, action_space)
        self.action_space = action_space
        
    def forward(self, state, action):
        state = torch.tensor(state).float()
        state_repr = self.icm_common(state).detach()
        action = torch.tensor(action)
        action = F.one_hot(action, self.action_space)
        x = torch.cat((state_repr, action), dim = 0)
        next_state_pred = self.icm_forward(x)
        return next_state_pred
    
    def inverse(self, state, next_state):    
        state = torch.tensor(state).float()
        next_state = torch.tensor(next_state).float()
        state_repr = self.icm_common(state)
        next_state_repr = self.icm_common(next_state)
        x = torch.cat((state_repr, next_state_repr),dim = 0)
        action_pred = F.softmax(self.icm_inverse(x), dim = 0)
        return action_pred
    




class ICM():
    def __init__(self, state_space, action_space, representation_size, lr = 1e-4):
        self.network = ICM_network(state_space, action_space, representation_size)
        self.forward_optimizer = torch.optim.Adam(self.network.parameters(), lr = lr)
        self.inverse_optimizer = torch.optim.Adam(self.network.parameters(), lr = lr)
        self.action_space = action_space
        
    def train_step(self, state, next_state, action):
        '''
        Inverse Pass
        '''
        action_pred = self.network.inverse(state, next_state)
        criterion = nn.NLLLoss()
        loss = criterion(action_pred.unsqueeze(0), torch.tensor([action]))
        loss.backward()
        self.inverse_optimizer.step()
        self.inverse_optimizer.zero_grad()
        
        '''
        Forward Pass
        '''
        next_state_pred = self.network(state, action)
        next_state = torch.tensor(next_state).float()
        next_state_repr = self.network.icm_common(next_state)
        criterion = nn.MSELoss()
        curiosity = criterion(next_state_pred, next_state_repr)
        curiosity.backward()
        self.forward_optimizer.step()
        self.forward_optimizer.zero_grad()
        return curiosity.detach()



    
class RND_Networks(nn.Module):
    def __init__(self , state_space, hidden_size, representation_size):
        super(RND_Networks, self).__init__()
        self.fc1 = nn.Linear(state_space, hidden_size)
        self.fc2 = nn.Linear(hidden_size, representation_size)
    def forward(self, state):
        state = torch.tensor(state).float()
        x = self.fc1(state)
        x = self.fc2(x)
        return x



class RND():
    def __init__(self, state_space, hidden_size, representation_size, lr = 1e-4):
        self.random_network = RND_Networks(state_space, hidden_size, representation_size)
        self.predictor_network = RND_Networks(state_space, hidden_size, representation_size)
        self.optimizer = torch.optim.Adam(self.predictor_network.parameters(), lr = lr)
    
    def train_step(self, next_state):
        x_target = self.random_network(next_state).detach()
        x_pred = self.predictor_network(next_state)
        criterion = nn.MSELoss()
        curiosity = criterion(x_pred, x_target)
        curiosity.backward()
        self.optimizer.step()
        self.optimizer.zero_grad()
        return curiosity

