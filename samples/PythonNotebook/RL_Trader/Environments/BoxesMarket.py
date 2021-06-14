#!/usr/bin/env python
# coding: utf-8


from Environments.BaseMarket import MarketEnv
import numpy as np
from gym import spaces
import matplotlib.pyplot as plt

# Example configuration
# conf = {'data': 'Data/ground_truth/',
#         'starting_money': 1000,
#         'starting_stocks': 0,
#         'episode_length': 10000,
#         'commission': 0.0025,
#         'state_trades_num': 10,
#         'action_mode':1
#         }

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


class BoxesMarket(MarketEnv):
    
    def __init__(self, env_config):
        self.state_trades_num = env_config['state_trades_num']
        self.action_mode = env_config['action_mode']
        super().__init__(env_config)
        
    def reset(self,):
        self.trades = []    
        self.state_trades = []
        self.last_market_beater = 0
        state = super().reset()
        print('-'*30 + "Reset called" + '-'*30)
        
        return state
    
    def test(self,):
        self.trades = []    
        self.state_trades = []
        self.last_market_beater = 0
        state = super().test()
        print('-'*30 + "Test called" + '-'*30)        

        return state
    
    def modify_high_low(self):           
        self.high = np.append(self.high, [101, 2000, 1000])
        self.low = np.append(self.low, [-1, -1, -1])
            
#       We add boundary values for a number of trades to keep track off
#       We add two slots for all the trades left, ripe unripe respectively      

        state_trades_lows = [-1]*(self.state_trades_num+2)
        state_trades_highs = [np.inf]*(self.state_trades_num+2)
            
        self.low = np.append(self.low, state_trades_lows)
        self.high = np.append(self.high, state_trades_highs)
        
    def set_action_space(self,):
        if self.action_mode == 1:
            self.action_space = spaces.Discrete(4) # buy/sell 10/50/100% + hold 
        elif self.action_mode == 2:
            self.action_space = spaces.Tuple((spaces.Discrete(4), spaces.Box(low=0.5, high=1.0, shape=(1,))))
    
    def modify_state(self, state):
        not_liquid_assets = self.current_price * self.current_stocks
        percentage_in_cash = 100 * (self.current_money/(self.current_money + not_liquid_assets))
        state = np.append(state, [percentage_in_cash, self.current_money, not_liquid_assets])
        self.update_state_trades()
        state = np.append(state, self.state_trades)
        
        return state
        
    def extract_action_info(self, action):
        buy_coeffs = {0:0.01,
                      1:0.02,
                      2:0.05
        }
        action_info = {}
        if self.action_mode == 1:
#           Determining action type
            if action in [0, 1, 2]:
                action_type = 'buy'
                buy_coeff = buy_coeffs[action]
                action_info['buy_coeff'] = buy_coeff     
            elif action == 3:
                action_type = 'sell'
                
        if self.action_mode == 2:
#           Determining action type
            if action[0] in [0, 1, 2]:
                action_type = 'buy'
                buy_coeff = buy_coeffs[action[0]]
                action_info['buy_coeff'] = buy_coeff
            elif action[0] == 3:
                action_type = 'sell'
            box_coeff = action[1]
            action_info['box_coeff'] = box_coeff
                
        action_info['action_type'] = action_type
        
        return action_info
        
    def do_action(self, action_info):
        reward = 0
        action_type = action_info['action_type']
        done = 0
        self.update_trade_states(self.current_price)
        
        if self.action_mode == 1:
            if action_type == 'buy':
                buy_coeff = action_info['buy_coeff']
                amount = self.current_money * buy_coeff
                buy_info = self.buy(amount)
            elif action_type == 'sell':
                buy_rewards_info = self.calculate_buy_rewards(self.current_price)
                reward = self.sell_ripe(self.current_price)
                
        if self.action_mode == 2:
            box_coeff = action_info['box_coeff']
            if action_type == 'buy':
                buy_coeff = action_info['buy_coeff']
                amount = self.current_money * buy_coeff
                self.buy(amount, box_coeff)
            elif action_type == 'sell':
                buy_rewards_info = self.calculate_buy_rewards(self.current_price)
                reward = self.sell_ripe(self.current_price)
        
        self.tick += 1
        if self.tick == len(self.data)-1:
            done = 1
        self.current_price = self.data['price'][self.tick]
        next_state = self.data.iloc[self.tick].values
        next_state = self.modify_state(next_state)
        next_state = super().normalize_state(next_state)
        
        new_market_beater = super().calculate_market_beater()
        market_beater_delta = new_market_beater - self.last_market_beater
        self.last_market_beater = new_market_beater
        
        reward = market_beater_delta
        assets = self.current_money + self.current_price * self.current_stocks
            
        return next_state, reward, done, {'current_price': self.current_price,
                                          'assets': assets,
                                          'market_beater': self.calculate_market_beater()}
    
    def buy(self, amount, *args):
        trade_info = super().buy(amount)
        
        stock_amount = trade_info[0]
        
        if self.action_mode == 1:
            trade = Trade(self.current_price, stock_amount, self.tick)
        if self.action_mode == 2:
            box_coeff = args[0]
            trade = Trade(self.current_price, stock_amount, self.tick, box_coeff)
            
        self.trades.append(trade)
            
        return trade_info
    
    def trade_delta(self, trade, price):
        delta = trade.amount*(price - trade.buy_price)  

        return delta
    
    def sell_ripe(self, sell_price):        
        reward = 0
        counter = 0
        for trade in self.trades:
            if trade.is_ripe():
                reward += self.trade_delta(trade, sell_price)
                self.current_money += trade.amount*sell_price*(1-self.commission)
                self.current_stocks -= trade.amount
                self.trades.remove(trade)
                counter += 1
#         print("Closed {} positions, accumulated reward: {}   Remaining stock: {}\n".format(counter, reward, self.current_stocks))
        
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

