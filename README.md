# Test Mate


This is a simple UI based solution to execute Automation Test Cases written using Visual Studio 2012

Pre-Requisite:

1. Visual Studio 2012 Ultimate
2. Test Cases automated using MSTest V1 Projects
3. SQL for storing Test Run
4. TFS Project access
5. Splunk & Sharepoint for ROI (Return on Investment) calculation


Description:

Use this solution to execute automated test cases locally using MSTest V1.

Developers and QA Manual Testers could use this to run their automation test cases to validate functionalities without needing to connect to TFS or MTM. 

This solution does not publish results to MTM or TFS. Locally a trx could be found in the execution location. 

SQL needs to be configured with certain schema to use this. Schema could be conceived by looking into Queries written in the C# Code.
