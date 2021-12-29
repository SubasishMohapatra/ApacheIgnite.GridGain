# ApacheIgnite.GridGain
 Sample example to demonstrate usage of Apache Ignite and GridGain
**SetUp**
Server Installation (On Premises)
Follow this link for detail explanation: REST API for GridGain | GridGain Documentation
	1. Download GridGain Community Edition binary from here
	2. Unzip the zip archive into the installation folder in your system.
	3. Move the ignite-rest-http folder from {gridgain}/libs/optional to {gridgain}/libs to enable the Ignite REST library for the cluster. The library is used by GridGain Web Console for cluster management and monitoring needs.
	4. (Optional) Enable required modules.
	5. (Optional) Set the IGNITE_HOME environment variable or Windows PATH to point to the installation folder and make sure there is no trailing / (or \ for Windows) in the path.
	6. Once that’s done, you will need to enable HTTP connectivity. To do this, copy the ignite-rest-http module from {gridgain_dir}/libs/optional/ to the {gridgain_dir}/libs folder.
Starting a GridGain Node
Navigate into the bin folder of GridGain installation directory from the command shell. Your command might look like this:
cd{gridgain installation folder}\bin\

Start a node with a custom configuration file that is passed as a parameter to ignite.sh|bat like this:
**.\ignite.bat ..\examples\config\example-ignite.xml**

You will see output similar to this:
[08:53:45] Ignite node started OK (id=7b30bc8e)
[08:53:45] Topology snapshot [ver=1, locNode=7b30bc8e, servers=1, clients=0, state=ACTIVE, CPUs=4, offheap=1.6GB, heap=2.0GB]

Open another tab from your command shell and run the same command again:
**ignite.bat ..\examples\config\example-ignite.xml**

Check the Topology snapshot line in the output. Now you have a cluster of two server nodes with more CPUs and RAM available cluster-wide:

[08:54:34] Ignite node started OK (id=3a30b7a4)
[08:54:34] Topology snapshot [ver=2, locNode=3a30b7a4, servers=2, clients=0, state=ACTIVE, CPUs=4, offheap=3.2GB, heap=4.0GB]
Running Your First GridGain Application
Once the cluster is started, you can use the GridGain REST API to perform cache operations.
You don’t need to explicitly configure anything because the connector is initialized automatically, listening on port 8080.
To verify the connector is ready, use curl:
Install Curl using Chocolatey
Why Chocolatey? Ease of use in installation/deployment and automation of software and packages.
Follow this link for detail on Chocolatey installation: Chocolatey Software | Installing Chocolatey
Install Chocolatey
	1. Launch Powershell or Terminal in administrator mode and run below command:
Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex ((New-Object System.Net.WebClient).DownloadString(' https://community.chocolatey.org/install.ps1'))
Install Curl
Run this command:
	**choco install curl**
Navigate to curl folder: **cd C:\ProgramData\chocolatey\lib\curl\tools\curl-7.80.0-win64-mingw\bin**
Launch curl
**.\curl "http://localhost:8080/ignite?cmd=version"**

One should see a message like this:
**.\curl "http://localhost:8080/ignite?cmd=version"{"successStatus":0,"error":null,"sessionToken":null,"response":"8.8.12"}**

Create a cache:

**.\curl "http://localhost:8080/ignite?cmd=getorcreate&cacheName=myCache"**

Put data into the cache. 
The default type is "string" but you can specify a data type via the keyType parameter.
**.\curl "http://localhost:8080/ignite?cmd=put&key=1&val='Hello_World!'&cacheName=myCache"**
Get the data from the cache
**.\curl " http://localhost:8080/ignite?cmd=get&key=1&cacheName=myCache"**
