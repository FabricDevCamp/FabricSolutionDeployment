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

# create products table for silver layer
from pyspark.sql.types import StructType, StructField, StringType, LongType, FloatType

# create schema for products table using StructType and StructField 
schema_products = StructType([
    StructField("ProductId", LongType() ),
    StructField("Product", StringType() ),
    StructField("Category", StringType() )
])

# Load CSV file into Spark DataFrame and validate data using schema
df_products = (
    spark.read.format("csv")
         .option("header","true")
         .schema(schema_products)
         .load("Files/sales-data/Products.csv")
)

# save DataFrame as lakehouse table in Delta format
( df_products.write
             .mode("overwrite")
             .option("overwriteSchema", "True")
             .format("delta")
             .save("Tables/silver_products")
)

# display table schema and data
df_products.printSchema()
df_products.show()

# CELL ********************

# create customers table for silver layer
from pyspark.sql.types import StructType, StructField, StringType, LongType, DateType

# create schema for customers table using StructType and StructField 
schema_customers = StructType([
    StructField("CustomerId", LongType() ),
    StructField("FirstName", StringType() ),
    StructField("LastName", StringType() ),
    StructField("Country", StringType() ),
    StructField("City", StringType() ),
    StructField("DOB", DateType() ),
])

# Load CSV file into Spark DataFrame with schema and support to infer dates
df_customers = (
    spark.read.format("csv")
         .option("header","true")
         .schema(schema_customers)
         .option("dateFormat", "MM/dd/yyyy")
         .option("inferSchema", "true")
         .load("Files/sales-data/Customers.csv")
)

# save DataFrame as lakehouse table in Delta format
( df_customers.write
              .mode("overwrite")
              .option("overwriteSchema", "True")
              .format("delta")
              .save("Tables/silver_customers")
)

# display table schema and data
df_customers.printSchema()
df_customers.show()

# CELL ********************

# create invoices table for silver layer
from pyspark.sql.types import StructType, StructField, LongType, FloatType, DateType

# create schema for invoices table using StructType and StructField 
schema_invoices = StructType([
    StructField("InvoiceId", LongType() ),
    StructField("Date", DateType() ),
    StructField("TotalSalesAmount", FloatType() ),
    StructField("CustomerId", LongType() )
])

# Load CSV file into Spark DataFrame with schema and support to infer dates
df_invoices = (
    spark.read.format("csv")
         .option("header","true")
         .schema(schema_invoices)
         .option("dateFormat", "MM/dd/yyyy")
         .option("inferSchema", "true") 
         .load("Files/sales-data/Invoices.csv")
)

# save DataFrame as lakehouse table in Delta format
( df_invoices.write
             .mode("overwrite")
             .option("overwriteSchema", "True")
             .format("delta")
             .save("Tables/silver_invoices")
)

# display table schema and data
df_invoices.printSchema()
df_invoices.show()

# CELL ********************

# create invoice_details table for silver layer
from pyspark.sql.types import StructType, StructField, LongType, FloatType

# create schema for invoice_details table using StructType and StructField 
schema_invoice_details = StructType([
    StructField("Id", LongType() ),
    StructField("Quantity", LongType() ),
    StructField("SalesAmount", FloatType() ),
    StructField("InvoiceId", LongType() ),
    StructField("ProductId", LongType() )
])

# Load CSV file into Spark DataFrame and validate data using schema
df_invoice_details = (
    spark.read.format("csv")
         .option("header","true")
         .schema(schema_invoice_details)
         .load("Files/sales-data/InvoiceDetails.csv")
)

# save DataFrame as lakehouse table in Delta format
( df_invoice_details.write
                    .mode("overwrite")
                    .option("overwriteSchema", "True")
                    .format("delta")
                    .save("Tables/silver_invoice_details")
)

# display table schema and data
df_invoice_details.printSchema()
df_invoice_details.show()
