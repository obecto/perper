FROM python:3.9

# Install Perper and Python dependencies

RUN git clone --depth 1 https://github.com/bojidar-bg/ignite-python-thin-client/ -b xx-perper-fixes /home/patched-ignite-python-thin-client
RUN pip install /home/patched-ignite-python-thin-client

COPY agent/python /home/perper/agent/python
RUN pip install -e /home/perper/agent/python

# COPY perper/samples/container_sample/requirements.txt /home/perper/samples/container_sample/requirements.txt
# RUN pip install -r /home/perper/samples/container_sample/requirements.txt

# Copy rest of files

COPY samples/python/container_sample /home/perper/samples/python/container_sample

WORKDIR /home/perper/samples/python/container_sample

CMD [ "python3", "main.py" ]
