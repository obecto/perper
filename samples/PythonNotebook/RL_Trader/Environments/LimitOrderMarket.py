#!/usr/bin/env python
# coding: utf-8

import numpy as np
from gym import spaces

from Environments.BaseMarket import MarketEnv

# Example config
# 
# conf = {'data': 'Data/ground_truth/',
#         'starting_money': 1000,
#         'starting_stocks': 0,
#         'episode_length': 10000,
#         'commission': 0.0025,
#         'state_orders_num': 10,
#         'max_horizon' : 1000,
#         'curiosity_reward' : 0
#         }



                

class Order():
    def __init__(self, order_info):
        self.action_type = order_info['action_type']
        self.amount = order_info['amount']
        self.at_price = order_info['at_price']
        self.time_horizon = order_info['time_horizon']
        self.age = 0
        self.expired = False
        
    def update_state(self):
        if self.age >= self.time_horizon:
            self.expired = True          
        self.age += 1
    
    def is_expired(self):
        
        return self.expired
    
    def is_executable(self, price):
        
        if self.action_type == 'sell':
            if self.at_price <= price:
                return True
            else:
                return False
            
        if self.action_type == 'buy':
            if self.at_price >= price:
                return True
            else:
                return False

class LimitMarket(MarketEnv):
    def __init__(self, env_config):
        self.state_orders_num = env_config['state_orders_num']
        self.max_horizon = env_config['max_horizon']
        super().__init__(env_config)
        
    def modify_high_low(self,):
        
#       Append boundaries for percent of money is cash, cash, stocks value, cash locked and stocks locked
        self.high = np.append(self.high, [101, 2000, 1000, 2000, 1000])
        self.low = np.append(self.low, [-1, -1, -1, -1, -1])
            
#       We add boundary values for a number of orders to keep track off
#       We add two slots for all the orders left, valid and expired      

        state_orders_lows = [-1]*(self.state_orders_num+2)
        state_orders_highs = [np.inf]*(self.state_orders_num+2)
            
        self.low = np.append(self.low, state_orders_lows)
        self.high = np.append(self.high, state_orders_highs)
    
    def reset(self):
        self.orders = []    
        self.state_orders = []
        self.last_market_beater = 0
        self.blocked_cash = 0
        self.blocked_stocks = 0
        state = super().reset()
        print('-'*30 + "Reset called" + '-'*30)
        
        return state
    
    def test(self):
        self.orders = []    
        self.state_orders = []
        self.last_market_beater = 0
        self.blocked_cash = 0
        self.blocked_stocks = 0
        state = super().test()
        print('-'*30 + "Test called" + '-'*30)

        return state
    
    def set_action_space(self,):
        self.action_space = spaces.Tuple((spaces.Discrete(7), spaces.Box(low = 0, high=1, shape=(1,)), spaces.Discrete(4)))
    
    def modify_state(self, state):
        not_liquid_assets = self.current_price * self.current_stocks
        percentage_in_cash = 100 * (self.current_money/(self.current_money + not_liquid_assets))
        cash_free = self.current_money - self.blocked_cash
        stocks_free = (self.current_stocks - self.blocked_stocks) * self.current_price
        
        state = np.append(state, [percentage_in_cash, self.current_money, not_liquid_assets])
        
        self.update_state_orders()
        state = np.append(state, self.state_orders)
        
        return state
    
    def extract_action_info(self, action):
        coeff_trade_dict = {0: 0.1,
                      1: 0.2,
                      2: 0.5,
                      3: 0.01,
                      4: 0.02,
                      5: 0.05}
        
        coeff_order_dict = {0: 0,
                            1: 0.0025,
                            2: 0.005,
                            3: 0.01}
        
        try:
            coeff_order = coeff_order_dict[action[2]]
            coeff_trade = coeff_trade_dict[action[0]]
        except:
            action_type = 'hold'
        
        if action[0] == 6:
            return {'action_type': action_type}
        if action[0] in [0, 1, 2]:
            action_type = 'buy'
            order_price = self.current_price * (1 - coeff_order)
            amount = (self.current_money - self.blocked_cash) * coeff_trade / order_price
        if action[0] in [3, 4, 5]:
            action_type = 'sell'
            order_price = self.current_price * (1 + coeff_order)
            amount = (self.current_stocks - self.blocked_stocks) * coeff_trade
        
        time_horizon = int(self.max_horizon * action[1])
            
        action_info = {'action_type': action_type,
                       'at_price': order_price,
                       'amount': amount,
                       'time_horizon': time_horizon}
        return action_info
    
    def do_action(self, action_info):
        done = 0
        reward = 0
        
        self.update_order_states()
        action_type = action_info['action_type']
        
        if action_type == 'hold':
            pass
        elif action_type == 'sell' and self.current_stocks - self.blocked_stocks == 0:
            reward -= 1
            print("Bad sell")
            pass
        elif action_type == 'buy' and self.current_money - self.blocked_cash == 0:
            reward -= 1
            print("Bad buy")
            pass
        else:
            self.place_order(action_info)
        
        punishment = self.remove_expired_orders()
        executed = self.execute_orders()
        
        new_market_beater = super().calculate_market_beater()
        market_beater_delta = new_market_beater - self.last_market_beater
        self.last_market_beater = new_market_beater
        
        self.tick += 1
        if self.tick == len(self.data)-1:
            done = 1
        self.current_price = self.data['price'][self.tick]
        next_state = self.data.iloc[self.tick].values
        next_state = self.modify_state(next_state)
        next_state = super().normalize_state(next_state)
        
        reward += market_beater_delta
        assets = self.current_money + self.current_price * self.current_stocks
        
        info = {'current_price': self.current_price,
                                          'assets': assets,
                                          'market_beater': super().calculate_market_beater()}
#         print("Free money: {}".format(self.current_money - self.blocked_cash))
#         print("Free stocks value: {}".format((self.current_stocks - self.blocked_stocks) * self.current_price))
#         print('{}\n'.format(info))
        
        return next_state, reward, done, info
    
    def place_order(self, order_info):
        order = Order(order_info)
        self.orders.append(order)
        if order.action_type == 'buy':
            self.blocked_cash += order.amount * order.at_price
        if order.action_type == 'sell':
            self.blocked_stocks += order.amount
    
    def remove_expired_orders(self):
        punishment = 0
        for order in self.orders:
            if order.is_expired():
                punishment += 1
                if order.action_type == 'buy':
                    self.blocked_cash -= order.amount * order.at_price
                if order.action_type == 'sell':
                    self.blocked_stocks -= order.amount
                self.orders.remove(order)
        return punishment
    
    def execute_orders(self):
        executed = 0
        for order in self.orders:
            if order.is_executable(self.current_price):
                executed += 1 
                if order.action_type == 'sell':
                    super().sell(order.amount)
                    self.blocked_stocks -= order.amount
                if order.action_type == 'buy':
                    super().buy(order.amount)
                    self.blocked_cash -= order.amount * order.at_price
                self.orders.remove(order)
        return executed
    
    def update_order_states(self):
        for order in self.orders:
            order.update_state()
    
#   When there are not enough orders made yet fill the left slots with zeros
    def extend_with_zeros(self):
        assert self.state_orders_num >= len(self.orders)
        zeros_extension = np.zeros(self.state_orders_num - len(self.orders)+2)
        self.state_orders = np.append(self.state_orders, zeros_extension)

    def fill_main_slots(self):
        assert self.state_orders_num < len(self.orders)
        for trade in self.orders[::-1][:self.state_orders_num]:
            self.state_orders.append(trade.amount*trade.at_price)
    
    def update_state_orders(self, mode='volume'):                
#       Calculate the sums for the orders outside the main slots
        def get_buy_sell_sums(orders):
            sell_volumes = []
            buy_volumes = []
            for order in orders:
                if order.action_type == 'sell':
                    sell_volumes.append(order.amount*order.at_price)
                if order.action_type == 'buy':
                    buy_volumes.append(order.amount*order.at_price)
                    
            if not sell_volumes:
                sell_sum = 0
            else:
                sell_sum = sum(sell_volumes)
                
            if not buy_volumes:
                buy_sum = 0
            else:
                buy_sum = sum(buy_volumes)
            
            return sell_sum, buy_sum
        
#       Fill the main slots with respect to mode and then fill the two slots remaining
        self.state_orders = []
        orders_copy = self.orders
        
        if mode == 'time':
            pass
                
        if mode == 'volume':
            orders_copy.sort(key=lambda x:x.amount*x.at_price)
            
#       If there are not enough orders made yet extend with zeroes
        if len(orders_copy) <= self.state_orders_num:
            for order in orders_copy[::-1]:
                self.state_orders = np.append(self.state_orders, order.amount*order.at_price)
            self.extend_with_zeros()
            
#       Fill main slots and calculate means for the two other slots
        else:
            self.fill_main_slots()
            orders_left = orders_copy[:-self.state_orders_num]
            sell_sum_left, buy_sum_left = get_buy_sell_sums(orders_left)
            self.state_orders = np.append(self.state_orders, [sell_sum_left, buy_sum_left])

        if len(self.state_orders) != self.state_orders_num + 2:
            print("The orders to include in the state have an incorrect format")
        
