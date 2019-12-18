# Bulk labels replacer for Dynamics 365 for Operations
Application replacing specified label with meaningful value and updating source code using xRefs

Algorithm is following:
1. Specify "Model store folder path" and "SQL connection string"
2. Specify label name to be replaced and new label name
3. Application will queue DYNAMICSXREFDB database to collect xReferences
4. If nothing found, then specified label is abandoned, else
5. Find XML source file which contain looking label
6. If no files found, then xReferences is out of date, and you have to rebuild application with marked "Build cross reference data", else
7. Replace all entries of old lable to new lable and save the XML source file
