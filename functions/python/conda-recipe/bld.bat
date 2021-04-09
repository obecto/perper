pip install https://files.pythonhosted.org/packages/de/80/e7df6f3366df8c582d309e6a623a4cd09f3a1bd9da0e3e3301faed0589ee/pyignite-0.3.4-py3-none-any.whl
pip install https://files.pythonhosted.org/packages/6b/0f/db22101e9f98b8db09f8df57ebe1c39933a239301ea89a9fb69241bbad49/azure_functions_worker-1.1.9-py3-none-any.whl
if errorlevel 1 exit 1
"%PYTHON%" setup.py install --single-version-externally-managed --record=record.txt
if errorlevel 1 exit 1