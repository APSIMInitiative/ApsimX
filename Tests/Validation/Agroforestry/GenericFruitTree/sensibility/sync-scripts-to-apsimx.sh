#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
export SCRIPT_DIR

node <<'NODE'
const fs = require('fs');
const path = require('path');

const dir = process.env.SCRIPT_DIR;
const apsimxPath = path.join(dir, 'GenericFruitTreeSensibility.apsimx');
const orchardScriptPath = path.join(dir, 'OrchardManagementScript.cs');
const postSimulationTestsPath = path.join(dir, 'PostSimulationTests.cs');

function normaliseNewlines(text) {
    return text.replace(/\r\n/g, '\n').replace(/\r/g, '\n').replace(/\n?$/, '\n');
}

function findNode(node, predicate) {
    if (!node || typeof node !== 'object')
        return null;
    if (predicate(node))
        return node;

    for (const value of Object.values(node)) {
        if (Array.isArray(value)) {
            for (const item of value) {
                const found = findNode(item, predicate);
                if (found)
                    return found;
            }
        } else if (value && typeof value === 'object') {
            const found = findNode(value, predicate);
            if (found)
                return found;
        }
    }
    return null;
}

const apsimx = JSON.parse(fs.readFileSync(apsimxPath, 'utf8'));
const orchardManagement = findNode(
    apsimx,
    node => node.Name === 'OrchardManagement' && Array.isArray(node.CodeArray));
const postSimulationTests = findNode(
    apsimx,
    node => node.Name === 'PostSimulationTests' && typeof node.Code === 'string');

if (!orchardManagement)
    throw new Error('Could not find OrchardManagement manager with CodeArray.');
if (!postSimulationTests)
    throw new Error('Could not find PostSimulationTests manager with Code.');

const orchardScript = normaliseNewlines(fs.readFileSync(orchardScriptPath, 'utf8'));
const postSimulationCode = normaliseNewlines(fs.readFileSync(postSimulationTestsPath, 'utf8'));

orchardManagement.CodeArray = orchardScript.replace(/\n$/, '').split('\n');
postSimulationTests.Code = postSimulationCode.replace(/\n$/, '');

fs.writeFileSync(apsimxPath, JSON.stringify(apsimx, null, 2) + '\n');
NODE
