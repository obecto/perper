from Environments.BaseMarket import MarketEnv
from gym import spaces
import numpy as np

conf = {'curiosity_reward':150,
        'continuous':False,
        'data': 'Data/ground_truth/',
        'starting_money': 1000,
        'starting_stocks': 0,
        'episode_length': 10000,
        'commission': 0.0025
        }
        
class SimpleMarket(MarketEnv):
    def __init__(self, env_config):
        self.is_continuous = env_config['continuous']
        super().__init__(env_config)
        
    def modify_high_low(self,):
        self.high = np.append(self.high, [100., 2000., 1000., self.high[-1]])
        self.low = np.append(self.low, [0., 0., 0., self.low[-1]])
        
    def set_action_space(self,):
        if self.is_continuous == False:
            self.action_space = spaces.Discrete(7) # buy/sell 10/50/100% + hold
        else:
            self.action_space = spaces.Box(high = 1, low = -1, shape = (1,))
        return self.action_space
    
    def modify_state(self, state):
        not_liquid_assets = self.current_price * self.current_stocks
        percentage_in_cash = 100 * (self.current_money/(self.current_money + not_liquid_assets))
        state = np.append(state, [percentage_in_cash, self.current_money, not_liquid_assets, self.zero_line])
        return state
    
    async def reset(self,):
        self.zero_line = 0
        state = await super().reset()

        print(f"Reset SimpleMarket before return: {state}")

        return state.copy()
    
#     Not actually implemented
#     def test(self, ):
#         self.zero_line = 0
#         state = super().test()
#         return state
    
    def extract_action_info(self, action):
        if self.is_continuous:            
            if action > 0:
                action_type = 'buy'
            elif action == 0:
                action_type = 'hold'
            elif action < 0:
                action_type = 'sell'
            percentage = abs(action[0])
        else:
            action_dict = {0:0,
                           1:0.1,
                           2:0.2,
                           3:0.5,
                           4:0.1,
                           5:0.5,
                           6:1
                }
            if action == 0:
                action_type = 'hold'
            elif action < 4:
                action_type = 'buy'
            else:
                action_type = 'sell'
         
            percentage = action_dict[action]
        if action_type == 'sell' and self.current_stocks == 0:
            action_type = 'hold'
        elif action_type == 'buy' and self.current_money == 0:
            action_type = 'hold'
        return (action_type, percentage)
    
    def calculate_reward(self, transaction_info):
        stock_delta = transaction_info[0]
        cash_delta = transaction_info[1]
        if stock_delta > 0:
            reward = self.commission * cash_delta
        elif stock_delta < 0:
            reward = (self.current_price * (1 - self.commission) - self.zero_line) * abs(stock_delta)
        else:
            reward = 0
        return reward 
    
    def update_zero_line(self, buy_info):
        stock_delta = buy_info[0]
        stocks_before_trade = self.current_stocks - stock_delta
        self.zero_line = (self.zero_line * stocks_before_trade + stock_delta * self.current_price)/(stocks_before_trade + stock_delta)
        
    def normalize_state(self, state):
        normalized_state = (state - self.low)/(self.high - self.low + 1e-17)
        
        return normalized_state
    
    def calculate_market_beater(self):
        market_delta = (self.potential_initial_stock * self.current_price-self.starting_money)/self.starting_money
        asset_delta =  (self.current_money + self.current_stocks * self.current_price - self.starting_money)/self.starting_money
        return asset_delta - market_delta
    
    def do_action(self, action_info):
        done = 0
        action_type, percentage = action_info
        if action_type == 'hold':
            trade_info = [0,0]
            reward = self.calculate_reward(trade_info)
        elif action_type == 'buy':
            amount = percentage * self.current_money
            trade_info = self.buy(amount)
            reward = self.calculate_reward(trade_info)
            self.update_zero_line(trade_info)
        elif action_type == 'sell':
            amount = percentage * self.current_stocks
            trade_info = self.sell(amount)
            reward = self.calculate_reward(trade_info)

        if self.current_stocks == 0:
            self.zero_line = 0     
        
        self.tick += 1
        if self.tick == len(self.data)-1:
            done = 1
        self.current_price = self.data['price'][self.tick]
        next_state = self.data.iloc[self.tick].values
        next_state = self.modify_state(next_state)
        next_state = self.normalize_state(next_state)
        return next_state, reward, done, {}