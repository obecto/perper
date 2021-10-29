1. Install jupyterlab and required packages

```
$ pip install jupyterlab
```

2. Install `perper`

```
$ git clone -b layer-restructure https://github.com/obecto/perper
$ pip install -e perper/agent/python/perper
```

3. Run fabric:

```
$ cd perper/fabric
$ ./gradlew run --args=config/example.xml
```

4. Run jupyter

```
$ jupyterlab
```
