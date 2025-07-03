## Overview

Messenger is an all in one library for handling communication within and between programs.
Requests and messages are sent to an `ISender` and then handled by an `IHandler`. By default this will all be routed within the same application but if you want to handle requests 
in a different service than where it was sent you can write "Forwarders" to pass the message along and then handle it in service containing the handler by passing the data to the `IRouter`.
The framework supports 3 types of data detailed in the section below.

- Messages - These are fire and forget messages that do not expect a response. Think sending a message on something like rabbitMQ.
- Requests - These are request response messages that expect the data to comeback inline. Think making an HTTP request.
- Async Requests - These are request response messages but the response can be receved asynchronously to the request. Good example is [here](https://www.rabbitmq.com/tutorials/tutorial-six-dotnet) but think a work queue but i want to message back when its done.

In my opinion these broadly cover all of the options someone might want to communicate in a distributed application.

> The primary purpose of this library is to easily facilitate the splitting and merging of services in a distributed system. use this library when you build a monolith and turning it into microservices (and back again!) becomes trivial(at least the messaging part does).
## Usage

The app consists of creating the following things
- Requests/Messages objects
- If its a Request youll need a Response Object
- Youll need a handler to run the code you want when the request is sent
- If you want your request to cross between services youll need to make a forwarder to send it
- If youve made a forwarder youll need to add some code on the receiving side to receive the data sent by the forwarder. This will need to 
  - receive it and unpack it
  - route it through the IRouter
  - Send the response back if its a request

### Registration
The Library will need to be added to all services that intend to send or receive data. The library is design to be used with microsofts dependency injection libraries.
This is mainly to work with aspnet as its what most people will use. It can be used otherwise by manually setting up a `var services = new ServiceCollection()` and doing `services.BuildServiceProvider()` to get the sender and router.

Register Messenger with
```C#
services.AddMessenger(o => {
	o.RegisterAssemblies(/*Assemblies here*/);
})
```
Pass in any assemblies that contain handlers or forwarders you want to use.

If aspnet configuration is setup the library will look in the `Messaging` section to find forwarder config. EG to work with app settings
```JSON
// From the Demo client project appsettings

  "Messaging": {
    "Forwarders": {
      "DemoMessageForwarder": [ "TestMessage" ],
      "DemoRequestForwarder": [ "AddRequest" ],
      "DemoAsyncRequestForwarder": [ "StringLowerRequest" ]
    }
  },
```

By default strings map to the name of the class but if you want to override to avoid conflicts the `Messenger.NameAttribute` Attribute can be added to classes to define the string that is matched by the json config (works for the requests and forwarders). Ensure names match if you are duplicating request/message objects between programs.

If you arent using aspnet configuration you can load it in manually with 
```C#
services.AddMessenger(o => {
	o.RegisterAssemblies(/*Assemblies here*/);
    o.Load(/*In code version of the above json config*/)
})
```

### Sending data
Sending data is easy just get an `ISender` from the service provider (in aspnet this will come from ctor injection). Then send your messages or requests and if necessary await to get the result.
This should be the simplest part the framework id built around making this easy.

### Receiving data

Write a handler. An IHandler with one Generic Parameter is for messages `IHandler<TestMessage>` and if its got 2 thats for requests `IHandler<TestRequest, TestResponse>`.
Then you can fill out what you want to happen when the request/message is sent.

The handler can be in the same service as the thing that sends the request or in a different program entirely.

Ensure you add the assembly containing any handlers when when register the library.

### Creating forwarders
If your sending information between services youll need to make a forwarder. These can inherit from 1 or more of 
- `IMessageForwarder`
- `IRequestForwarder`
- `IAsyncRequestForwarder` - if you register a single implmentation for both `IRequestForwarder` and `IAsyncRequestForwarder` then `IRequestForwarder` will take precendence

Create Server code on the receiving service to accept whatever protocol the forwarder passes the message in. The server should have the required handler to deal with the message and use the `IRouter` from the service provider to handle the message.

## Demo
This solution contains a small Demo App outlining the 3 types of messages. They are all in the Demo folder
- Client - An application that launches a background task to send requests when it launches. Its also a webserver so it can handle the response for an Async Request
- Server - The server that the client can forward requests to to prove out the forwarders. Is is setup to handle all types of requests and send the response that matches the type of request
  - If the handler returned a response then its not a message
  - If the request came with an id then its an async request
  - Otherwise its a synchronous request
- Demo.Common - Used by Client and Server and contains all the requests and handlers

> In real world environments you wouldnt handle all types of reuests just the ones your expecting to receive.

All handlers are registered in both client and server to demo how config can be used to easily move between handling a message in the same service or forwarding it to another.

The client contains 3 Forwarders for each type of request.
- DemoMessageForwarder
- DemoRequestForwarder
- DemoAsyncRequestForwarder

Running the demo involves starting the server and then while its running starting the client. To see the flexiblity of the system try changing which messages are passed to which forwarder
- `IMessages` like `TestMessage` can be passed to an `IMessageForwarder` or not configured to any forwarder and it will try to resolve to handler in the same program.
- `IRequests` like `AddRequest` and `StringLowerRequest` can be configured to be sent to an `IRequestForwarder`, `IAsyncRequestForwarder` or nothing to resolve in the same program.

Try changing the appsettings for the client and running it to see which applicaiton the logs pop up in.


