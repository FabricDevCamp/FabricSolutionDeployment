# Synapse Analytics notebook source

# METADATA ********************

# META {
# META   "synapse": {
# META     "lakehouse": {
# META       "default_lakehouse": "{LAKEHOUSE_ID}",
# META       "default_lakehouse_name": "{LAKEHOUSE_NAME}",
# META       "default_lakehouse_workspace_id": "{WORKSPACE_ID}",
# META       "known_lakehouses": [
# META         {
# META           "id": "{LAKEHOUSE_ID}"
# META         }
# META       ]
# META     }
# META   }
# META }

# CELL ********************

# copy CSV files to lakehouse to load data into bronze layer 
import requests

csv_files_base_url = "https://github.com/FabricDevCamp/SampleData/raw/refs/heads/main/ProductSales/"

csv_files = { "Customers.csv", "Products.csv", "Invoices.csv", "InvoiceDetails.csv" }

folder_path = "Files/bronze_landing_layer/"

for csv_file in csv_files:
    csv_file_path = csv_files_base_url + csv_file
    with requests.get(csv_file_path) as response:
        csv_content = response.content.decode('utf-8-sig')
        mssparkutils.fs.put(folder_path + csv_file, csv_content, True)
        print(csv_file + " copied to Lakehouse file in OneLake")
