import express from 'express';
import bodyParser from 'body-parser';
import fs from 'fs';

const app = express();
app.use(bodyParser.json({ limit: '50mb' }));

let printStart = 0;
let curPause = false;
let pausedTime = 0;

app.post('/slice', (req, res) => {
    const gcodePath = "model.gcode";
    fs.readFile(gcodePath, async (err: Error, data: Buffer) => {
        if (err) {
            console.error(`Error reading GCODE file: ${err}`);
            res.status(500).json({ message: 'Internal server error' });
            return;
        }
        setTimeout(() => res.status(200).json({ gcodePath: gcodePath, gcode: data.toString()}), 5000);
    });
});

app.post('/print', async (req, res) => {
    let checkFunction = async () => {
        printStart = Date.now();
        res.status(200).json({ message: 'Success', uuid: "15080f25-5c8d-4781-a473-1ef13483078e" });
    };

    setTimeout(() => checkFunction(), 5000);
});

app.get('/print', async (req, res) => {
    let elapsed = Date.now() - printStart;
    if(curPause){
        elapsed = pausedTime;
    }
    if (elapsed < 10000){
        res.status(200).json({"datetime_cleaned":"","datetime_finished":"","datetime_started":"2024-08-10T17:58:10","name":"model","pause_source":"","progress":0,"reprint_original_uuid":"","result":"","source":"WEB_API","source_application":"TableCAD","source_user":"PrintTask","state":"pre_print","time_elapsed":0,"time_total":0,"uuid":"15080f25-5c8d-4781-a473-1ef13483078e"});
    } else if (elapsed < 20000) {
        res.status(200).json({"datetime_cleaned":"","datetime_finished":"","datetime_started":"2024-08-10T17:58:10","name":"model","pause_source":"","progress":0.890022723027678,"reprint_original_uuid":"","result":"","source":"WEB_API","source_application":"TableCAD","source_user":"PrintTask","state":"printing","time_elapsed":213,"time_total":239,"uuid":"15080f25-5c8d-4781-a473-1ef13483078e"});
    } else if (elapsed < 30000) {
        res.status(200).json({"datetime_cleaned":"","datetime_finished":"2024-08-10T18:06:21","datetime_started":"2024-08-10T17:58:10","name":"model","pause_source":"","progress":1,"reprint_original_uuid":"","result":"","source":"WEB_API","source_application":"TableCAD","source_user":"PrintTask","state":"wait_cleanup","time_elapsed":241,"time_total":239,"uuid":"15080f25-5c8d-4781-a473-1ef13483078e"});
    } else {
        res.status(200).json({"datetime_cleaned":"2024-08-10T18:49:15","datetime_finished":"2024-08-10T18:48:17","datetime_started":"2024-08-10T18:40:20","name":"model","pause_source":"","progress":0,"reprint_original_uuid":"","result":"Aborted","source":"","source_application":"","source_user":"","state":"wait_user_action","time_elapsed":0,"time_total":0,"uuid":"b3ef48e0-b3c6-4caf-80c6-83247adb2fcd"});
    }
});

app.put('/print', async (req, res) => {
    const printerIP = req.body.printerIP;
    switch (req.body.state) {
        case "continue":
            if(curPause){
                printStart = Date.now() - pausedTime;
                curPause = false;
            }
            break;
        case "pause":
            if(!curPause){
                pausedTime = Date.now() - printStart;
                curPause = true;
            }
            break;
        case "stop":
            printStart = 0;
            curPause = false;
            break;
        default:
            res.status(400).json({ message: "Invalid state" });
            return;
    }
    console.log("Changed");
    res.status(200).json({ message: 'Success' });
});

app.listen(3000, "0.0.0.0", () => {
    console.log('Server running on port 3000');
});
