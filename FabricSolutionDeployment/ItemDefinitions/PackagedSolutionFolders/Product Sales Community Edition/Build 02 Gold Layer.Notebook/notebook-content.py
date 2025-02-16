# Fabric notebook source

# METADATA ********************

# META {
# META   "dependencies": {
# META     "lakehouse": {
# META       "default_lakehouse": "126dbed9-b038-4ab4-ac5a-0f2e9051e448",
# META       "default_lakehouse_name": "sales",
# META       "default_lakehouse_workspace_id": "0613d70d-5e4c-407f-98a0-af5a7365c035",
# META       "known_lakehouses": [
# META         {
# META           "id": "126dbed9-b038-4ab4-ac5a-0f2e9051e448"
# META         }
# META       ]
# META     }
# META   }
# META }

# CELL ********************

# create products table for gold layer

# load DataFrame from silver layer table
df_gold_products = (
    spark.read
         .format("delta")
         .load("Tables/silver_products")
)

# write DataFrame to new gold layer table 
( df_gold_products.write
                  .mode("overwrite")
                  .option("overwriteSchema", "True")
                  .format("delta")
                  .save("Tables/products")
)

# display table schema and data
df_gold_products.printSchema()
df_gold_products.show()

# CELL ********************

# create customers table for gold layer
from pyspark.sql.functions import concat_ws, floor, datediff, current_date, col

# load DataFrame from silver layer table and perform transforms
df_gold_customers = (
    spark.read
         .format("delta")
         .load("Tables/silver_customers")
         .withColumn("Customer", concat_ws(' ', col('FirstName'), col('LastName')) )
         .withColumn("Location", concat_ws(', ', col('City'), col('Country')) )
         .withColumn("Age",( floor( datediff( current_date(), col("DOB") )/365.25) ))   
         .drop('FirstName', 'LastName')
)

# write DataFrame to new gold layer table 
( df_gold_customers.write
                   .mode("overwrite")
                   .option("overwriteSchema", "True")
                   .format("delta")
                   .save("Tables/customers")
)

# display table schema and data
df_gold_customers.printSchema()
df_gold_customers.show()

# CELL ********************

# create sales table for gold layer
from pyspark.sql.functions import col, desc, concat, lit, floor, datediff
from pyspark.sql.functions import date_format, to_date, current_date, year, month, dayofmonth

# load DataFrames using invoices table and invoice_details table from silver layer
df_silver_invoices = spark.read.format("delta").load("Tables/silver_invoices")
df_silver_invoice_details = spark.read.format("delta").load("Tables/silver_invoice_details")

# perform join to merge columns from both DataFrames and transform data 
df_gold_sales = (
    df_silver_invoice_details
        .join(df_silver_invoices, 
              df_silver_invoice_details['InvoiceId'] == df_silver_invoices['InvoiceId'])
        .withColumnRenamed('SalesAmount', 'Sales')
        .withColumn("DateKey", (year(col('Date'))*10000) + 
                               (month(col('Date'))*100) + 
                               (dayofmonth(col('Date')))   )
        .drop('InvoiceId', 'TotalSalesAmount', 'InvoiceId', 'Id')
        .select('Date', "DateKey", "CustomerId", "ProductId", "Sales", "Quantity")
)

# write DataFrame to new gold layer table 
( df_gold_sales.write
               .mode("overwrite")
               .option("overwriteSchema", "True")
               .format("delta")
               .save("Tables/sales")
)

# display table schema and data
df_gold_sales.printSchema()
df_gold_sales.show()

# CELL ********************

# create calendar table for gold layer
from datetime import date
import pandas as pd
from pyspark.sql.functions import to_date, year, month, dayofmonth, quarter, dayofweek, date_format

# get first and last calendar date from sakes table 
first_sales_date = df_gold_sales.agg({"Date": "min"}).collect()[0][0]
last_sales_date = df_gold_sales.agg({"Date": "max"}).collect()[0][0]

# calculate start date and end date for calendar table
start_date = date(first_sales_date.year, 1, 1)
end_date = date(last_sales_date.year, 12, 31)

# create pandas DataFrame with Date series column
df_calendar_ps = pd.date_range(start_date, end_date, freq='D').to_frame()

# convert pandas DataFrame to Spark DataFrame and add calculated calendar columns
df_calendar_spark = (
     spark.createDataFrame(df_calendar_ps)
       .withColumnRenamed("0", "timestamp")
       .withColumn("Date", to_date(col('timestamp')))
       .withColumn("DateKey", (year(col('timestamp'))*10000) + 
                              (month(col('timestamp'))*100) + 
                              (dayofmonth(col('timestamp')))   )
       .withColumn("Year", year(col('timestamp'))  )
       .withColumn("Quarter", date_format(col('timestamp'),"yyyy-QQ")  )
       .withColumn("Month", date_format(col('timestamp'),'yyyy-MM')  )
       .withColumn("Day", dayofmonth(col('timestamp'))  )
       .withColumn("MonthInYear", date_format(col('timestamp'),'MMMM')  )
       .withColumn("MonthInYearSort", month(col('timestamp'))  )
       .withColumn("DayOfWeek", date_format(col('timestamp'),'EEEE')  )
       .withColumn("DayOfWeekSort", dayofweek(col('timestamp')))
       .drop('timestamp')
)

# write DataFrame to new gold layer table 
( df_calendar_spark.write
                   .mode("overwrite")
                   .option("overwriteSchema", "True")
                   .format("delta")
                   .save("Tables/calendar")
)

# display table schema and data
df_calendar_spark.printSchema()
df_calendar_spark.show()
