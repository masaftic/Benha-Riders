import signalR from "@microsoft/signalr";

function createConnection(hubUrl, jwtToken) {
  return new signalR.HubConnectionBuilder()
    .withUrl(hubUrl, {
      accessTokenFactory: () => jwtToken
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();
}

export { createConnection };
