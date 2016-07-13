/// <reference path="jquery-1.7.1.js" />
/// <reference path="raphael.js" />

var _pointattr = { fill: "#fff", stroke: "black", "stroke-width": "2" };
var _ellipseAttr = { fill: "#fff", "fill-opacity": "0.3", stroke: "black", "stroke_width": "2" };
var _xHandleAttr = { fill: "#ff0", cursor: "e-resize" };
var _yHandleAttr = { fill: "#ff0", cursor: "s-resize" };
var _textAttr = { "font-size": "16" };
var _viewportWidth = 1600;
var _viewportHeight = 900;
var _boxPaddingX = 12;
var _boxPaddingY = 6;
var _defaultXRadius = 100;
var _defaultYRadius = 80;
var _handleRadius = 4;
var _pointRadius = 10;

function imageMove(dx, dy) {
    if (this.labelCanvas._mode == "pan") {
        this.labelCanvas._isDragging = true;
        this.update(dx - (this.dx || 0), dy - (this.dy || 0));
        this.dx = dx;
        this.dy = dy;
    }
}
function imageUp() {
    if (this.labelCanvas._mode == "pan") {
        this.dx = this.dy = 0;
        this.labelCanvas._isDragging = false;
    }
}

function handleMove(dx, dy) {
    if (this.labelCanvas._mode == "select" || this.labelCanvas._mode == "ellipse") {
        this.update(dx - (this.dx || 0), dy - (this.dy || 0));
        this.dx = dx;
        this.dy = dy;
    }
}
function handleDown() {
    if (this.labelCanvas._mode == "select" || this.labelCanvas._mode == "ellipse") {
        this.dx = this.dy = 0;
    }
}
function handleUp() {
    if (this.labelCanvas._mode == "select" || this.labelCanvas._mode == "ellipse") {
        this.dx = this.dy = 0;
        this.labelCanvas.updatePoint(this.pointId);
    }
}

function pointMove(dx, dy) {
    if (this.labelCanvas._mode == "select" || this.labelCanvas._mode == "point" || this.labelCanvas._mode == "ellipse") {
        this.labelCanvas._isDragging = true;
        this.update(dx - (this.dx || 0), dy - (this.dy || 0));
        this.dx = dx;
        this.dy = dy;
        this.labelCanvas._selectedId = this.node.id;
        this.attr("fill", "#f00");
    }
}

function pointDown() {
    if (this.labelCanvas._mode == "select" || this.labelCanvas._mode == "point" || this.labelCanvas._mode == "ellipse") {
        var labelCanvas = this.labelCanvas;
        if (labelCanvas._rPoints[labelCanvas._selectedId])
            labelCanvas._rPoints[labelCanvas._selectedId].attr("fill", "#fff");

        if (this.node.id == labelCanvas._selectedId)
            labelCanvas._selectedId = "";
        else {
            this.attr("fill", "#f00");
            labelCanvas._selectedId = this.node.id;
            labelCanvas._tag = labelCanvas._points[labelCanvas._selectedId].tag;
            $.event.trigger({
                type: "pointSelected",
                message: { tag: labelCanvas._tag },
                time: new Date()
            });
        }
    }
}
function pointUp(event) {
    if (this.labelCanvas._mode == "select" || this.labelCanvas._mode == "point" || this.labelCanvas._mode == "ellipse") {
        var labelCanvas = this.labelCanvas;
        this.dx = this.dy = 0;
        labelCanvas.updatePoint(this.node.id, event.pageX, event.pageY);
    }
}

function LabelCanvas(translateX, translateY, scale, imageInfo) {
    this.tag = "LabelCanvas";
    this._translateX = translateX;
    this._translateY = translateY;
    this._scale = scale * .1;
    this._imageInfo = imageInfo;
    this._points = {};
    this._rPoints = {};
    this._handles = {};
    this._text = {};
    this._isDragging = false;
    this._selectedId = "";
    this._mode = "select";
    this._src = $("#image").attr("src");
    this._tag = "Unknown";
    $("#holder").html("");
    this._R = Raphael("holder", _viewportWidth, _viewportHeight);
    this._img = this._R.image(this._src, (_viewportWidth-imageInfo.width)/2, (_viewportHeight-imageInfo.height)/2, imageInfo.width, imageInfo.height);
    this._img.labelCanvas = this;
    this._idLookup = new Array();

    this._img.update = function(dx,dy){       
        var labelCanvas = this.labelCanvas;
        dx = dx / labelCanvas._scale;
        dy = dy / labelCanvas._scale;
        labelCanvas._translateX += dx;
        labelCanvas._translateY += dy;
        labelCanvas.updateImage();
    };
    this._img.click(function(event)
    {
        var labelCanvas = this.labelCanvas;
        if (labelCanvas._mode == "point" || labelCanvas._mode == "ellipse") {            
            var bb = this.getBBox();
            var imageX = event.pageX - $("#holder")[0].offsetLeft - bb.x;
            var imageY = event.pageY - $("#holder")[0].offsetTop - bb.y;
            var pointX = imageX / labelCanvas._scale;
            var pointY = imageY / labelCanvas._scale;
            if (pointX >= 0 && pointX < labelCanvas._imageInfo.width && pointY >= 0 && pointY < labelCanvas._imageInfo.height) {
                if (labelCanvas._mode == "ellipse")
                    labelCanvas.addPoint(pointX, pointY, _defaultXRadius, _defaultYRadius, true);
                else labelCanvas.addPoint(pointX, pointY);
            }
        }
    });
    this._img.drag(imageMove, imageUp);
}

LabelCanvas.prototype.deletePoint = function (pointId) {
    if (pointId == null)
        pointId = this._selectedId;
    if (pointId == null)
        return;

    var point = this._points[pointId];
    var pointParams = {
        id: pointId,
        x: point.x,
        y: point.y,
        xRadius: point.xRadius,
        yRadius: point.yRadius,
        isEllipse: point.isEllipse,
        translateX: this._translateX,
        translateY: this._translateY,
        scale: this._scale * 10,
        imageInfoId: this._imageInfo.id,
        tag: point.tag
    };

    $.ajax({
        url: $("#deleteLabelURL").val() + "?" + $.param(pointParams),
        type: "POST"
    }).done(function (response) {
        if (point.isEllipse) {
            labelCanvas._handles[response.id].remove();
            delete labelCanvas._handles[response.id];
        }
        delete labelCanvas._points[response.id];
        labelCanvas._rPoints[response.id].remove();
        delete labelCanvas._rPoints[response.id];
        labelCanvas._text[response.id].remove();
        delete labelCanvas._text[response.id];
        labelCanvas.updateImage();
    });
}

LabelCanvas.prototype.updatePoint = function(pointId,pageX,pageY)
{
    var point = this._points[pointId];
    var pointParams = {
        id: pointId,
        x: point.x,
        y: point.y,
        xRadius: point.xRadius,
        yRadius: point.yRadius,
        isEllipse: point.isEllipse,
        translateX: this._translateX,
        translateY: this._translateY,
        scale: this._scale * 10,
        imageInfoId: this._imageInfo.id,
        tag: point.tag
    };
    var isOffCanvas = false;
    if (!(typeof pageX === 'undefined')) {
        var holder = $("#holder")[0];
        var holderX = pageX - holder.offsetLeft;
        var holderY = pageY - holder.offsetTop;
        if (holderX < 0 || holderX >= holder.offsetWidth || holderY < 0 || holderY >= holder.offsetHeight)
            isOffCanvas = true;
    }

    var labelCanvas = this;
    if(isOffCanvas)
    {
        this.deletePoint(pointId);
    } else {
        $.ajax({
            url: $("#updateLabelURL").val() + "?" + $.param(pointParams),
            type: "POST"
        });
    }
}

LabelCanvas.prototype.updateImage = function()
{
    this._img.transform("s" + this._scale + "t" + this._translateX + "," + this._translateY);
    var box = this._img.getBBox();
    var labelCanvas = this;
    $.each(labelCanvas._points, function (key, value) {
        var pointX = box.x + labelCanvas._scale * value.x;
        var pointY = box.y + labelCanvas._scale * value.y;
        if (value.isEllipse) {
            labelCanvas._rPoints[key].transform("t" + pointX + "," + pointY + "s" + labelCanvas._scale);
            var xDiff = value.xRadius * labelCanvas._scale;
            var yDiff = value.yRadius * labelCanvas._scale;
            labelCanvas._handles[key][0].transform("t" + (pointX + xDiff) + "," + pointY);
            labelCanvas._handles[key][1].transform("t" + pointX + "," + (pointY + yDiff));
        }
        else {
            labelCanvas._rPoints[key].transform("t" + pointX + "," + pointY);
        }
        pointY += value.yRadius * labelCanvas._scale;
        labelCanvas._text[key].transform("t" + pointX + "," + pointY);
    });
};

LabelCanvas.prototype.setTag = function (tag) {
    this.setTagById(this._selectedId, tag);
}

LabelCanvas.prototype.setTagById = function (id, tag) {
    var text = this._text[id];
    var point = this._points[id];
    this._tag = tag;
    if (text) {
        text[1].attr("text", tag);
        point.tag = tag;
        var bb = text[1].getBBox();
        if (point.isEllipse) {
            text[0].attr({ x: (-bb.width - _boxPaddingX) / 2, y: 0, width: bb.width + _boxPaddingX, height: bb.height + _boxPaddingY });
        }
        else {
            text[0].attr({ x: (-bb.width - _boxPaddingX) / 2, y: 20 + -bb.height / 2, width: bb.width + _boxPaddingX, height: bb.height + _boxPaddingY });
        }
    }
}


LabelCanvas.prototype.addPoint = function (pointX, pointY, pointXRadius, pointYRadius, isEllipse) {
    pointXRadius = typeof pointXRadius === 'undefined' ? 0 : pointXRadius;
    pointYRadius = typeof pointYRadius === 'undefined' ? 0 : pointYRadius;
    isEllipse = typeof isEllipse === 'undefined' ? false : isEllipse;
    var pointParams = {
        x: pointX,
        y: pointY,
        xRadius: pointXRadius,
        yRadius: pointYRadius,
        isEllipse: isEllipse,
        translateX: this._translateX,
        translateY: this._translateY,
        scale: this._scale * 10,
        imageInfoId: this._imageInfo.id,
        tag: this._tag
    };

    var labelCanvas = this;

    $.ajax({
        url: $("#createLabelURL").val() + "?" + $.param(pointParams),
        type: "POST"
    }).done(function (response) {
        var id = response.id;
        if (isEllipse)
            labelCanvas.registerEllipse(id, pointX, pointY, pointXRadius, pointYRadius, labelCanvas._tag);
        else labelCanvas.registerPoint(id, pointX, pointY, labelCanvas._tag);
        if (labelCanvas._rPoints[labelCanvas._selectedId])
            labelCanvas._rPoints[labelCanvas._selectedId].attr("fill", "#fff");
        labelCanvas._selectedId = id;
        labelCanvas._rPoints[labelCanvas._selectedId].attr("fill", "#f00");
    });
}

LabelCanvas.prototype.registerPoint = function (id, pointX, pointY, pointTag) {
    this._points[id] = { x: pointX, y: pointY, xRadius: 0, yRadius: 0, isEllipse: false, tag: pointTag };
    this._rPoints[id] = this._R.circle(0, 0, _pointRadius).attr(_pointattr);
    this._rPoints[id].node.id = id;
    this._rPoints[id].labelCanvas = this;
    this._rPoints[id].update = function (dx, dy) {
        var labelCanvas = this.labelCanvas;
        var point = labelCanvas._points[this.node.id];
        dx = dx / labelCanvas._scale;
        dy = dy / labelCanvas._scale;
        point.x += dx;
        point.y += dy;
        labelCanvas.updateImage();
    };
    this._rPoints[id].drag(pointMove, pointDown, pointUp);
    this._rPoints[id].dblclick(function () {
        if(this.labelCanvas._mode == "select" || this.labelCanvas._mode == "point")
            $("#holder").trigger('labelInfo', id);
    });

    this._text[id] = this._R.set();
    var text = this._R.text(0, 0, pointTag).attr(_textAttr);
    var bb = text.getBBox();
    text.attr("y", 2 * _pointRadius + (bb.height - _boxPaddingY) / 2);
    var rect = this._R.rect((-bb.width - _boxPaddingX) / 2, 2 * _pointRadius + -bb.height / 2, bb.width + _boxPaddingX, bb.height + _boxPaddingY).attr({ fill: "#fff", stroke: "none" });
    text.toFront();
    this._text[id].push(rect, text);
    this._idLookup.push(id);

    this.updateImage();
}

LabelCanvas.prototype.setMode = function (mode) {
    this._mode = mode;
    if (mode == "pan") {
        this._img.attr("cursor", "move");
    } else {
        this._img.attr("cursor", "auto");
    }
}

LabelCanvas.prototype.setFilter = function (filter) {
    for (var i = 0; i < this._idLookup.length; i++) {
        var id = this._idLookup[i];
        var point = this._rPoints[id];
        var text = this._text[id];
        var handles = this._handles[id];
        var opacity = 0;
        if (i >= filter)
            opacity = 1;
        if (point != null) {
            point.attr("opacity", opacity);
            text.attr("opacity", opacity);
        }
        if (handles != null)
            handles.attr("opacity", opacity);
    }
}

LabelCanvas.prototype.registerEllipse = function(id, pointX, pointY, pointXRadius, pointYRadius, pointTag)
{
    this._points[id] = { x: pointX, y: pointY, xRadius: pointXRadius, yRadius: pointYRadius, isEllipse: true, tag: pointTag };

    this._rPoints[id] = this._R.ellipse(0, 0, pointXRadius, pointYRadius).attr(_ellipseAttr);
    this._rPoints[id].node.id = id;
    this._rPoints[id].labelCanvas = this;
    this._rPoints[id].update = function (dx, dy) {
        var labelCanvas = this.labelCanvas;
        var point = labelCanvas._points[this.node.id];
        dx = dx / labelCanvas._scale;
        dy = dy / labelCanvas._scale;
        point.x += dx;
        point.y += dy;
        labelCanvas.updateImage();
    };
    this._rPoints[id].drag(pointMove, pointDown, pointUp);
    this._rPoints[id].dblclick(function () {
        if (this.labelCanvas._mode == "select" || this.labelCanvas._mode == "ellipse")
            $("#holder").trigger('labelInfo', id);
    });

    this._handles[id] = this._R.set();

    var xHandle = this._R.circle(0, 0, _handleRadius).attr(_xHandleAttr);
    xHandle.labelCanvas = this;
    xHandle.pointId = id;
    xHandle.update = function(dx,dy){
        var labelCanvas = this.labelCanvas;
        var point = labelCanvas._points[this.pointId];
        var rPoint = labelCanvas._rPoints[this.pointId];
        dx = dx / labelCanvas._scale;
        point.xRadius += dx;
        rPoint.attr("rx", point.xRadius);
        labelCanvas.updateImage();
    };
    var yHandle = this._R.circle(0, 0, _handleRadius).attr(_yHandleAttr);
    yHandle.labelCanvas = this;
    yHandle.pointId = id;
    yHandle.update = function(dx,dy){
        var labelCanvas = this.labelCanvas;
        var point = labelCanvas._points[this.pointId];
        var rPoint = labelCanvas._rPoints[this.pointId];
        dy = dy / labelCanvas._scale;
        point.yRadius += dy;
        rPoint.attr("ry", point.yRadius);
        labelCanvas.updateImage();
    };
    this._handles[id].push(xHandle, yHandle);    
    this._handles[id].drag(handleMove, handleDown, handleUp);

    this._text[id] = this._R.set();
    var text = this._R.text(0, 0, pointTag).attr(_textAttr);
    var bb = text.getBBox();
    text.attr("y", _handleRadius + (bb.height + _boxPaddingY)/2);
    var rect = this._R.rect((-bb.width - _boxPaddingX) / 2, _handleRadius, bb.width + _boxPaddingX, bb.height + _boxPaddingY).attr({ fill: "#fff", stroke: "none" });
    text.toFront();
    this._text[id].push(rect, text);
    this._idLookup.push(id);

    this.updateImage();
}

LabelCanvas.prototype.setScale = function (scale) {
    this._scale = scale * .1;
    this.updateImage();
}

LabelCanvas.prototype.saveTag = function (tag) {
    if (this._selectedId) {
        var textParams = { id: this._selectedId, tag: tag };
        $.ajax({
            url: $("#tagURL").val() + "?" + $.param(textParams),
            type: "POST"
        });
    }
}


