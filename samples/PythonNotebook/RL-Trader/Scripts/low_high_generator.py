import pandas as pd
from numpy import save
import numpy as np

data = pd.read_csv('../Data/indicator_dataset.csv')

'''Remove time and labels from data and save lowest and highest value in the respective column'''
try:
    low = data.min().to_numpy()[1:-36]-1
    low = np.append(low, [-1, -1, -1])
    high = data.max().to_numpy()[1:-36]+1
    high = np.append(high, [101, +np.inf, +np.inf])

    save("../Data/low_values.npy", low)
    save("../Data/high_values.npy", high)
    print("Generated low and high values from dataset to use in ray")
    
except:
    print("You need to download indicator_dataset.csv")
    