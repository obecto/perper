FROM tensorflow/tensorflow:latest-gpu-jupyter
RUN curl -s https://repo.anaconda.com/miniconda/Miniconda3-latest-Linux-x86_64.sh > install.sh && \
    chmod +x install.sh && \
    ./install.sh -b -p && \
    rm install.sh && \
    echo PATH=$PATH:/root/miniconda3/bin >> ~/.bashrc && \
    /root/miniconda3/bin/conda init bash

# Market Predictor Environment
COPY market_predictor.yml .
RUN /root/miniconda3/bin/conda env create -f market_predictor.yml

# Market Predictor Environment
COPY rl.yml .
RUN /root/miniconda3/bin/conda env create -f rl.yml