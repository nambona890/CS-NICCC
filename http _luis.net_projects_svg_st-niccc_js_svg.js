/*
Release Info
-------------
Name        : ST-NICCC2000 SVG
Author      : Luis Gonzalez - lobster@luis.net
Music       : "checknobankh" by laxity/kefrens written 1992/93
Date        : April 19, 2019
Version     : 1.0                                               
Platform    : SVG

Based on the precalculated vector animation data from an old Atari ST demo by Oxygene
http://www.pouet.net/prod.php?which=1251

best viewed in Chrome

http://seenjs.io/demo-svg-canvas.html
todo: fix looping

References:
-----------
ST-NICCC COMPETITION
<http://arsantica-online.com/st-niccc-competition/>

Competition Forum Thread
<http://www.pouet.net/topic.php?which=11559>

MOD Player Code
<https://med.planet-d.net/demo/web/modplayer/>

MOD Music File
<https://modarchive.org/index.php?request=view_by_moduleid&query=36330>

JS Canvas Version by Friol
<http://dantonag.it/stniccc/goDemo.html>

SVG: Scalable Vector Graphics
<https://developer.mozilla.org/en-US/docs/Web/SVG>

Autoplay Policy changes
<https://developers.google.com/web/updates/2017/09/autoplay-policy-changes>

File loader based from pulkomandy
<https://github.com/pulkomandy/beniccc/blob/master/main.cpp>

*/

var D = document.documentElement;
var J = D.childNodes;
var globalFrameNum = 0;
var player;
var polygonCount = 0;
var frameList = [];
var colorPalette = [];
var polygonElement;

function updateScreen() {
	// we need to always clear the screen
	clearScreen(polygonCount);

	// create polygon nodes
	frameList[globalFrameNum++].forEach(function (curpol, index) {
		closePath(curpol[1], colorPalette[curpol[0]]);
		polygonCount = index;
	});

	// start at the beginning after reaching frames end
	if (globalFrameNum == frameList.length - 1) {
		globalFrameNum = 0;
	}
}

function closePath(pathInfo, curcolor) {
	polygonElement = document.createElementNS("http://www.w3.org/2000/svg", "polygon");
	for (A in a = {
		points: pathInfo,
		style: curcolor
	}) polygonElement.setAttribute(A, a[A]);
	D.appendChild(polygonElement);
}

function setText(str) {
	let textObj = document.querySelector("text");
	if (textObj) {
		textObj.innerHTML = str;
	}
}

function onLoad() {
	// Load polygon data
	loadData("media/scene1.bin");
}

function onClick() {
	if (player.ready && !player.playing) {
		// start the music
		player.play();

		// delete the placeholder screen + instructional text
		clearScreen(frameList[globalFrameNum].length + 1);

		// add first frame until 50ms kicks in
		updateScreen();

		// begin demo
		setInterval(updateScreen, 50);
	}
}

function clearScreen(numberOfPolyons) {
	// remove only the dynamically added elements
	for (var pd = 0; pd < numberOfPolyons + 1; pd++) {
		D.removeChild(D.lastElementChild);
	}
}

function loadMusic() {
	// Prepare MOD Player
	player = new Protracker();
	player.setautostart(false);
	player.setseparation(true);
	player.setamigatype(true);
	player.setrepeat(true);

	player.onReady = function () {
		setText('click to start');
	};

	// Load Music file
	setText("loading music");
	player.load("media/chcknbnk.mod");
}

// load module from url into local buffer
function loadData(url) {

	setText("loading " + url);
	var oReq = new XMLHttpRequest();
	oReq.open("GET", url, true);
	oReq.responseType = "arraybuffer";

	oReq.onload = function (oEvent) {
		var arrayBuffer = oReq.response;
		if (arrayBuffer) {
			var byteArray = new Uint8Array(arrayBuffer);
			var colorArray = new Array(16).fill(0);

			for (var i = 0; i < byteArray.length;) {
				var vertArray = [];
				var pointsArray = [];

				var flags = byteArray[i++];

				// Bit 0: Frame needs to clear the screen.
				var flag_clearScreen = bit_test(flags, 0);

				// Bit 1: Frame contains palette data.
				var flag_hasPalette = bit_test(flags, 1);

				// Bit 2: Frame is stored in indexed mode.
				var flag_isIndexed = bit_test(flags, 2);

				// If frame contains palette data
				if (flag_hasPalette) {
					// Colors are stored as words in Atari-ST format 00000RRR0GGG0BBB
					// 512 possible colors
					// 1 word Bitmask
					var bitMask = ntohs([byteArray[i++], byteArray[i++]], 0);
					colorArray = colorArray.slice();
					for (var x = 0; x < 16; x++) {
						if (bit_test(bitMask, 15 - x)) {
							colorArray[x] = ntohs([byteArray[i++], byteArray[i++]], 0);
						}
					}
				}

				if (flag_isIndexed) {
					// 1 byte Number of vertices (0-255)
					var numberVertices = byteArray[i++];
					for (var x = 0; x < numberVertices; x++) {
						// 1 byte X-position, 1 byte Y-position
						vertArray.push([byteArray[i++], byteArray[i++]]);
					}
				}

				// hi-nibble - 4 bits color-index
				// lo-nibble - 4 bits number of polygon vertices
				outerLoop:
				while (bits = byteArray[i++]) {
					switch (bits) {
						case 0xFE:		// End of frame and the stream skips to the next 64KB block
							i &= ~0xFFFF;
							i += 0x10000;
						case 0xFF:		// End of frame
						case 0xFD:		// End of stream
							break outerLoop;
						default:
							var points = []
							for (var ii = 0; ii < (bits & 0xF); ii++) {
								points.push(flag_isIndexed ? vertArray[byteArray[i++]] : [byteArray[i++], byteArray[i++]]);
							}

							var curcolor = stPaletteToHTMLRGB((bits & 0xF0) >> 4, colorArray);
							var colorIndex = colorPalette.indexOf(curcolor);

							if (colorIndex == -1) {
								colorPalette.push(curcolor);
								colorIndex = colorPalette.length;
							}

							pointsArray.push([colorIndex, points.join(" ")]);
					}
				}

				frameList.push(pointsArray);
			}
		}
		console.info('color palette', colorPalette);
		loadMusic();
	};

	oReq.send(null);
};

/**
 * Convert a 16-bit quantity (short integer) from network byte order to host byte order
 * (Big-Endian to Little-Endian).
 *
 * @param {Array|Buffer} b Array of octets or a nodejs Buffer to read value from
 * @param {number} i Zero-based index at which to read from b
 * @returns {number} number
 */
function ntohs(b, i) {
	return ((0xff & b[i]) << 8) |
		((0xff & b[i + 1]));
};

/**
 * Check if the n-th bit in a number is set from the right end.
 * Bits are counted from right to left.
 * 
 * @param {number} num integer to to check
 * @param {number} bit n-th bit to test
 * @returns {boolean} returns true if the bit is set; it returns false if it is not.
 */
function bit_test(num, bit) {
	return (num & (1 << bit));
}

/**
 * Convert palette from atari ST format to HTML RGB
 * 
 * @param {number} color index of the palette to use
 * @param {number[]} palette array of numbers containing colors
 * @returns {string} rgb style information 
 */
function stPaletteToHTMLRGB(color, palette) {
	var curcolor = palette[color & 0x0f];

	var blu3 = ((curcolor & 0x007)) & 0xff;
	var gre3 = ((curcolor & 0x070) >> 4) & 0xff;
	var red3 = ((curcolor & 0x700) >> 8) & 0xff;

	var realBlue = blu3 << 5;
	var realGreen = gre3 << 5;
	var realRed = red3 << 5;

	return `color: rgb(${realRed},${realGreen},${realBlue})`;
}

