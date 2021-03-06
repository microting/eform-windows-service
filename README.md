# eform-windows-service 
![Build Status](https://microtingas2017.visualstudio.com/_apis/public/build/definitions/5f551ab2-01ab-4204-8efa-06be93328bc1/2/badge)

1.  Download 'MicrotingService' from GitHub ('https://github.com/microting/eform-windows-service')

2.  Compile the project using Visual Studio

3.	Run the 'MicrotingServiceInstaller' program ('MicrotingServiceInstaller.exe')

4.	Check 'settings' in both databases. Likely no changes needed

5.  Copy the compiled files to 'c:/microtingservice/[name of the service, of your choice]'
	- for this sample 'Microting', so the resulting folder would be 'c:/microtingservice/microting'
	- this location can be altered, by changing the source code - this is not adwised or covered

6.  In that folder create the following folders:
	- log
	- input
	
7.  Create in the input folder a fil called 'sql_connection_sdkCore.txt' containing the SDK's database connection string
	- the SDK's database needs to be primed and configed correct per your needs
      
8.  IF you want to use the Outlook module, also create in the input folder a fil called 'sql_connection_outlook.txt' containing the Outlook's database connection string
	- the Outlook's database needs to be primed and configed correct per your needs

9.  Open a command prompt with admin rights   

10.  Navigate to the location of 'InstallUtil.exe', usually found here:

	cd C:/Windows/Microsoft.NET/Framework/v4.0.30319
   
11.  Use the following command to install the service:   

	InstallUtil.exe /servicename="Microting" C:/MicrotingService/Microting/MicrotingService.exe
	
	
	- if other service name used:
	
	InstallUtil.exe /servicename="[service name]" C:/MicrotingService/[service name]/MicrotingService.exe
	
12. Enter the name of the username and password of the user the service is going to be running as. IMPORTANT that this user has the needed rights
	- Tip: to see current users username - use 'whoami' in the command prompt
	
13. Open Windows services, and start the service
	
	-----
   
!!! LOG !!!   
	- In the log folder you can find the service's log. The log from the SDK and Outlook can be found in their own databases

   
!!! DEBUGGING !!!   
	- If you want to the service to start Visual Studio debugging, at the start of the service. Create in the input folder a fil called 'debug.txt'   
  
   
!!! UNINSTALL !!!
	- You can uninstall by using the following command:
	
	InstallUtil.exe /servicename="Microting" C:\MicrotingService\Microting\MicrotingService.exe -u
	
!!! Installer Building !!!

1. Build service code
2. In case if you modified installer CustomAction project build it
3. Build AllowMultipleVersionProject
4. Build or Rebuild Installer - it is impotant that this project should be built last( Wix toolset is required - http://wixtoolset.org/)
