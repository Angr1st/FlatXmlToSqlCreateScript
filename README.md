# FlatXmlToSqlCreateScript

This Repo contains a small tool to analyse [Steribase](http://www.steribase.de/) xml files and create the necessary create scripts for SQL Tables. Currently only supports MySQL and MariaDB due to differences in the Syntax of Primary and Foreign Key declaration.

## How to use

1. Install the .NET Core SDK
2. Build the Application
3. Call the Application via the commandline tool and pass in at least the path to the xml. Currently only primary keys that start with the node name and end with 'ID' can be automatically identified. So as a second parameter you are allowed to specify the path to a text file that contains the (node name;primary key;) line separated.
4. In the working directory should now exist a new txt file called 'createTables.txt'. This contains all create scripts plus on the bottom a line that contains the information how many different Tables will be created when executing this script.

## Example

This Repository contains a example primary key file to help you get the format correct.