#!/usr/bin/env python
# coding: utf-8

# In[ ]:


class Position():
    def __init__(self, bought_price, money, line = 0.005):
        self.bought_price = bought_price
        self.line = line
        self.up_line = (1 + self.line) * bought_price
        self.down_line = (1 - self.line) * bought_price
        self.volume = money/bought_price
    def step(self, current_price):
        #strategy step
        #returns if the position should be sold
        if current_price > self.up_line:
            self.up_line = (1 + self.line) * current_price
            self.down_line = (1 - self.line) * current_price
        elif current_price < self.down_line:
            return 1
        else:
            return 0 
    def sell(self, current_price):
        #returns earnings as current_price
        return self.volume * current_price
    
