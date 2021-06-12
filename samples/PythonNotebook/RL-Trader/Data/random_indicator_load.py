#!/usr/bin/env python
# coding: utf-8

# In[1]:


import numpy as np
import pandas as pd
import os
import random


# In[2]:


def random_train_load(dataset, ep_size = 1000,  train_size = 0.9):
    # train_size is percentage of the dataset reserved for train
    filesize = os.stat(dataset).st_size
    offset = int(train_size * random.randrange(filesize))
    f = open(dataset)
    columns = f.readline()
    columns = pd.Series(columns[:-1].split(','))
    f.seek(offset)                  
    f.readline()
    episode = []
    random_line = f.readline()      
    if len(random_line) == 0:      
        f.seek(0)
        random_line = f.readline()
    random_line = np.fromstring(random_line, dtype=float, sep=',')
    episode.append(random_line)
    for i in range(ep_size -1):
        random_line = f.readline() 
        random_line = np.fromstring(random_line, dtype=float, sep=',')
        episode.append(random_line)
    episode = np.array(episode)
    episode = pd.DataFrame(data= episode, columns= columns)
    episode = episode.iloc[:,1:-24]
    return episode


# In[5]:

# In[ ]:



