import express from 'express';
import bodyParser from 'body-parser';
import { exec } from 'child_process';
import fs from 'fs';
import FormData from 'form-data';
import axios, { AxiosResponse, AxiosError } from "axios";
import AxiosDigestAuth from '@mhoc/axios-digest-auth';
import { readFileSync } from 'fs';

let slicers = JSON.parse(readFileSync("slicer_options.json", "utf8"));

function find_name_index(arr, name) {
    for (let i = 0; i < arr.length; i++) {
        if (arr[i].name == name) {
            return i;
        }
    }
    return -1;
}

const app = express();
app.use(bodyParser.json({ limit: '50mb' }));

function defaultErrorDeal(res, reason: AxiosError) {
    if (reason.response) {
        console.log(reason.response.status, reason.response.data)
        res.status(reason.response.status).json(reason.response.data);
    } else {
        console.log(reason);
        res.status(500).json({ message: 'External access error' });
    }
}

app.get('/slice', (req, res) => {
    let printers = [];
    let index: any;
    for (index in slicers) {
        printers.push({
            "name": slicers[index]["name"],
            "label": slicers[index]["label"],
            "options": slicers[index]["options"]
        });
    }
    res.status(200).json(printers);
});

app.post('/slice', (req, res) => {
    const stlFileData = req.body.stlFileData;
    const modelName = req.body.modelName;
    const printer = req.body.printer;
    console.log(req.body.options);
    const options = req.body.options;
    const stlFilePath = `output/${modelName}.stl`; // Temporary file path

    // Decode base64-encoded STL data and write it to a file
    fs.writeFile(stlFilePath, Buffer.from(stlFileData, 'base64'), (err) => {
        if (err) {
            console.error(`Error writing STL file: ${err}`);
            res.status(500).json({ message: 'Internal server error' });
            return;
        }

        const gcodePath = `output/${modelName}.gcode`;

        let cmd: string = slicers[find_name_index(slicers, printer)]["cmd"];
        let index: any;
        const pri_ind = find_name_index(slicers, printer)
        for (index in slicers[pri_ind]["options"]) {
            const opt_base = slicers[pri_ind]["options"][index];
            const opt_name = opt_base["name"];
            //console.log(opt_base)
            if (opt_name in options) {
                cmd = cmd.replace(`%%${opt_name}%%`, options[opt_name]);
            } else {
                cmd = cmd.replace(`%%${opt_name}%%`, opt_base["default"]);
            }
        }

        cmd = cmd.replace(`%%gcode_path%%`, gcodePath);
        cmd = cmd.replace(`%%stl_path%%`, `output/Phone_Holder.stl`);

        // Run CuraEngine to slice the STL file
        exec(cmd, (error, stdout, stderr) => {
            if (error) {
                console.error(`Error: ${error.message}`);
                res.status(500).json({ message: 'Slicer error' });
                return;
            }/*
                if (stderr) {
                    console.error(`Stderr: ${stderr}`);
                    res.status(500).json({ message: 'Slicer error' });
                    return;
                }*/

            console.log(`G-code file saved at: ${gcodePath}`);
            fs.readFile(gcodePath, async (err: Error, data: Buffer) => {
                if (err) {
                    console.error(`Error reading GCODE file: ${err}`);
                    res.status(500).json({ message: 'Internal server error' });
                    return;
                }
                res.status(200).json({ gcodePath: gcodePath, gcode: data.toString() });
            });
        });
    });
});

let keyMap = new Map();

async function printNow(req, res) {
    const printerIP = req.body.printerIP;
    const modelName = req.body.modelName;
    const gcodePath = `output/${modelName}.gcode`; // Temporary file path
    const authVal = keyMap.get(printerIP);

    fs.readFile(gcodePath, async (err: Error, data: Buffer) => {
        if (err) {
            console.error(`Error reading GCODE file: ${err}`);
            res.status(500).json({ message: 'Internal server error' });
            return;
        }
        console.log("Sending print job")
        const form = new FormData();
        form.append('jobname', "model");

        form.append('file', data.toString(), {
            filename: `${modelName}.gcode`,
            contentType: 'text/x.gcode'
        });
        const raw_data = form.getBuffer().toString()

        const digestAuth = new AxiosDigestAuth({
            username: authVal.id,
            password: authVal.key
        });

        digestAuth.request({
            headers: {
                "Content-Type": `multipart/form-data; boundary=${form.getBoundary()}`,
                "Accept": "application/json",
                "Content-Length": raw_data.length.toString()
            },
            method: "POST",
            url: `http://${printerIP}/api/v1/print_job`,
            data: raw_data,
            maxBodyLength: 104857600, //100mb
            maxContentLength: 104857600, //100mb
        }).then((response) => {
            console.log("Success");
            res.status(200).json({ message: 'Success', uuid: (response.data as any).uuid });
        }, (reason) => {
            defaultErrorDeal(res, reason);
        });
    });
}

async function authenticate(req, res, callback) {
    const printerIP = req.body.printerIP;
    const authVal = keyMap.get(printerIP);

    console.log(printerIP);

    if (authVal) {
        console.log("Already authenticated");
        const digestAuth = new AxiosDigestAuth({
            username: authVal.id,
            password: authVal.key
        });
        const authRes = await digestAuth.request(
            {
                method: "GET",
                url: `http://${printerIP}/api/v1/auth/verify`
            }).catch((error) => {
                console.log(error)
                return undefined;
            });
        if (authRes?.status === 200) {
            callback();
            return;
        }
        console.log("Authentication expired. Retrying");
    } else {
        console.log("Not authenticated, sending request");
    }

    let checkFunction = async (authRes: AxiosResponse) => {
        axios.get(`http://${printerIP}/api/v1/auth/check/${authRes.data.id}`).then((response) => {
            if (response.data.message === "unknown") {
                console.log("Waiting");
                setTimeout(() => checkFunction(authRes), 1000);
                return;
            }
            if (response.data.message === "unauthorized") {
                console.log("Rejected");
                res.status(403).json({ ip: printerIP, message: "Rejected" });
                return;
            }

            keyMap.set(printerIP, authRes.data);
            console.log("Accepted");
            callback();
        }, (reason) => {
            defaultErrorDeal(res, reason);
        });
    };

    axios.post(`http://${printerIP}/api/v1/auth/request`, {
        application: "TableCAD",
        user: "PrintTask"
    }, {
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded'
        }
    }).then((authRes) => {
        console.log("Waiting for response...");
        setTimeout(() => checkFunction(authRes), 1000);
    }, (reason) => {
        defaultErrorDeal(res, reason);
    });
}

app.post('/print', async (req, res) => {
    authenticate(req, res, () => printNow(req, res));
});

app.get('/print', async (req, res) => {
    const printerIP = req.query.printerIP;
    axios.get(`http://${printerIP}/api/v1/print_job`).then((result) => {
        res.status(200).json(result.data);
    }, (reason) => {
        defaultErrorDeal(res, reason);
    });
});

app.put('/print', async (req, res) => {
    const printerIP = req.body.printerIP;

    let state = ""
    switch (req.body.state) {
        case "continue":
            state = "print"
            break;
        case "pause":
            state = "pause"
            break;
        case "stop":
            state = "abort"
            break;
        default:
            res.status(400).json({ message: "Invalid state" });
            return;
    }
    console.log(`Change status to: ${state}`);

    let sendFunction = async () => {
        const authVal = keyMap.get(printerIP);
        const digestAuth = new AxiosDigestAuth({
            username: authVal.id,
            password: authVal.key
        });

        digestAuth.request({
            headers: {
                'Content-Type': 'application/json',
                "Accept": "application/json"
            },
            method: "PUT",
            url: `http://${printerIP}/api/v1/print_job/state`,
            data: { target: state },
            maxBodyLength: 104857600, //100mb
            maxContentLength: 104857600, //100mb
        }).then((response) => {
            console.log("Changed");
            res.status(200).json({ message: 'Success' });
        }, (reason) => {
            defaultErrorDeal(res, reason);
        });
    }

    authenticate(req, res, () => sendFunction());
});

app.listen(3000, "0.0.0.0", () => {
    console.log('Server running on port 3000');
});
