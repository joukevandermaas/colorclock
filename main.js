const doThings = require("./dothings");

const ipAddress = "192.168.178.10";
const username = "t2enGIRzvDs-RPwDwjHvKlTo3cubMbwCfg6sNvND";
const seconds = 10;

const { api } = require("node-hue-api").v3;

(async () => {
  const authenticatedApi = await api.createLocal(ipAddress).connect(username);

  console.log("Connected to Hue Bridge");

  await doThings(authenticatedApi, seconds);
})();
