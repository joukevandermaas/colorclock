window.log = (obj) => console.log(JSON.parse(obj));

window.createInterval = (obj, methodName, interval) => {
    return setInterval(() => {
        obj.invokeMethodAsync(methodName);
    }, interval)
};

