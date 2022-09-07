# YARP Security

This project uses YARP (Yet Another Reverse Proxy) to offload authentication at a gateway so that downstream applications don't need to have any authentication code/logic.

## Trust Dev Certificate

A self-signed certificate has been generated using dotnet dev-certs. This must be trusted so that there are no SSL errors when running the solution. Follow these steps if using Windows:
- Double click on `aspnetapp.cer`
- Click 'Install Certificate'
- Select Store Location 'Current User'
- Select 'Place all certificates in the following store' then 'Browse'
- Select 'Trusted Root Certification Authorities'
- Accept the security warning to install

This same certificate is loaded into the docker containers and trusted so they can communicate. In production certificates should be loaded in a more secure way or offloaded to another reverse proxy/load balancer.

## Build & Run

The YARP gateway proxy uses SSL, therefore a self-signed certificate must be generated first:
```cmd
dotnet dev-certs https --clean
dotnet dev-certs https -ep %USERPROFILE%\.aspnet\https\aspnetapp.pfx -p password
dotnet dev-certs https --trust
```

The Docker containers can be built using the following:
```bash
docker-compose build
```

Run Docker containers:
```bash
docker-compose up
```