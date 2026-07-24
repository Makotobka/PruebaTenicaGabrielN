Debido a problemas con Docker, se levanto todo en ambientes locales bajo un arquitectura en malla. 
- La programacion define un random con un probabilidad de fallos del 50%.
- Al ser una arquitectura en malla, uno por uno van procesando pagos. Si falla un nodo el segundo toma el control y asi sucesivamente
- Existe un limite maximo de intentos por nodos configurado a 6.
- No se alcanzo a poner un Balanceador de carga entre los nodos



- Ejemplos de ejecucion en powersheel, con ajustes en appsettings.json

$env:ASPNETCORE_ENVIRONMENT="Development"

dotnet run --no-launch-profile -- `
  --urls=http://localhost:5001 `
  --Node:Id=node-1 `
  --Node:Url=http://localhost:5001 `
  --Node:Peers:0:Id=node-2 `
  --Node:Peers:0:Url=http://localhost:5002 `
  --Node:Peers:1:Id=node-3 `
  --Node:Peers:1:Url=http://localhost:5003 `
  --Simulacion:ForzarFalloPrimerIntento=true
  
  
$env:ASPNETCORE_ENVIRONMENT="Development"

dotnet run --no-launch-profile -- `
  --urls=http://localhost:5002 `
  --Node:Id=node-2 `
  --Node:Url=http://localhost:5002 `
  --Node:Peers:0:Id=node-1 `
  --Node:Peers:0:Url=http://localhost:5001 `
  --Node:Peers:1:Id=node-3 `
  --Node:Peers:1:Url=http://localhost:5003
  
$env:ASPNETCORE_ENVIRONMENT="Development"

dotnet run --no-launch-profile -- `
  --urls=http://localhost:5003 `
  --Node:Id=node-3 `
  --Node:Url=http://localhost:5003 `
  --Node:Peers:0:Id=node-1 `
  --Node:Peers:0:Url=http://localhost:5001 `
  --Node:Peers:1:Id=node-2 `
  --Node:Peers:1:Url=http://localhost:5002