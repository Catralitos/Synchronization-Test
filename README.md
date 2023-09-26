# Synchronization-Test

This is a small project I did, where I made a C# program to sync two folders. Essentially on launch, and then at a set period, anything in the source folder will be reflected in the replica folder. 

File and directory creations, deletions and modifications made in the source folder will reflect in the replica folder after the synchronization runs.

The programs arguments are: 

1. The path to the source folder
2. The path to the replica folder
3. The period (in seconds)
4. The path to the log file

All changes to the replica folder are logged on the console and onto the log file.
