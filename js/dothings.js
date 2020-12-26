const v3 = require("node-hue-api").v3;
const LightState = v3.lightStates.LightState;

const bri = Math.floor(0.8 * 255);
const yellow = { hue: 6790, sat: 254, bri };
const red = { hue: 65186, sat: 254, bri };
const green = { hue: 21001, sat: 254, bri };

/*
todo before ready:
1. human friendly way to trigger it
2. allow stopping early, restarting, etc.
  (allow concurrent use on different lights?)
3. choose light/scene/room/etc
4. error handling
5. make the flash more obvious

new features:
1. auto repeat timer

*/

module.exports = async (api, timerSeconds) => {
  const hektarId = 1;

  let halfWayPoint = timerSeconds / 2;
  let tenPercent = timerSeconds / 10;

  const state = new LightState().on(true);

  state.populate(green);
  await api.lights.setLightState(hektarId, state);

  console.log(`starting ${timerSeconds} second timer`);

  await timeout(halfWayPoint);

  state.populate(yellow);
  await api.lights.setLightState(hektarId, state);

  console.log(`halfway point, ${halfWayPoint} seconds left`);

  await timeout(halfWayPoint - tenPercent);

  state.populate(red);
  await api.lights.setLightState(hektarId, state);

  console.log(`10% left, ${tenPercent} seconds`);

  await timeout(tenPercent);

  state.alert("lselect");
  await api.lights.setLightState(hektarId, state);

  console.log("done");
};

function timeout(timeoutSeconds) {
  return new Promise((resolve) => {
    setTimeout(resolve, timeoutSeconds * 1000);
  });
}

async function printLights(api) {
  let lights = await api.lights.getAll();
  console.log(
    lights.map((l) => ({
      id: l.id,
      name: l.name,
    }))
  );
}
