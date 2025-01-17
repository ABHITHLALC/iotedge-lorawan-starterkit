'use strict';

const glob = require('glob');
const fs = require('fs');
const fse = require('fs-extra');
const path = require('path');
const esprima = require('esprima-next');

var args = process.argv.slice(2);
const srcDir = args[0] || './node_modules/lorawan-devices/vendor';
const dstDir = args[1] || './codecs';
const indexFilePath = path.join(dstDir, 'index.js');
const index = glob.sync(`**/*`,
  {
    cwd: srcDir,
    nodir: true,
    ignore: [
      '**/*.jpg',
      '**/*.png',
      '**/*.svg',
    ]
  }).map(f => {
    const srcPath = `${srcDir}/${f}`;
    const dstPath = `${dstDir}/${f}`;

    console.log(`Copying ${srcPath} to ${dstPath}`);
    fse.copySync(srcPath, dstPath);

    if (f.endsWith(".js")) {
      // Sniff a top-level declaration for a function named "decodeUplink"
      // and include the decoder if only one is found.
      const tree = esprima.parseScript(fs.readFileSync(srcPath).toString());
      if (tree.body.find(n => n.type === 'FunctionDeclaration' && n.id.name === 'decodeUplink')) {
        fs.appendFile(dstPath, '\nmodule.exports={decodeUplink};', function (err) {
          if (err) throw err;
          console.log(`Patching ${dstPath}`);
        });
        return dstPath;
      }
    }
  })
  .filter(f => f)
  .map(f => [path.basename(f).split('.')[0], path.relative(dstDir, f).replace(/\\/g, '/')]);

fs.writeFileSync(indexFilePath,
  `module.exports = {\n${index.map(([k, v]) => `${JSON.stringify(k)}: require(${JSON.stringify('./' + v)})`).join(',\n  ')}\n};\n`);
