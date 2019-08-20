# NLog.Extensions.AzureStorage.NamingPartition
An NLog extension to store your NLog entries in Azure Tables partitioned by year and month.

Why using this extension
======
Azure Table is a very interesting (and cheap) Azure feature if you want to store tracing information. Using the NLog extention

At this moment, there is no way to set a data retention period for the data in an Azure Table. This means that you need to write your own tool (as a background job, for example), to scan the tables and delete data older than the data retention period.
Deleting rows in Azure Tables is not straightforward as in any other relational database. Azure Tables allows you to delete entries in Azure Tables with some limitations:

1. You can delete no more than 100 entries in a single batch operation
2. All the entries you delete in a single batch must belong to the same partition

These two limitations makes the entries deletion a time consuming operation.
On the other side, it is really strainghtforward and fast to delete an entire table.

For this reason, a good approach for time series data is to partition data in different tables, based on time.

For example, in my case, I want to have a 1 year data retention and I applied the following strategy:

1. I patition all the data in separate tables, one for each month of the year
2. the NLog adapter automatically write the entry in the proper table, given the timestamp of the entry

In this way, my background job have only to delete the entire tables with an expired time retention.