import importlib
import math
import os

import bpy
from mathutils import Vector

import hero
import lowpoly
import models


def run(repo, previews=True):
    for module in (lowpoly, hero, models):
        importlib.reload(module)

    out = os.path.join(repo, "Assets", "_Project", "Art", "Models")
    preview_dir = os.path.join(repo, "Temp", "ModelPreviews")
    os.makedirs(out, exist_ok=True)
    os.makedirs(preview_dir, exist_ok=True)

    built = [hero.build()] + [make() for make in models.ALL]
    report = {}
    for model in built:
        report[model.name] = model.export(os.path.join(out, model.name + ".lpmesh"))

    if previews:
        _setup_preview_render()
        for model in built:
            _render_preview(model, built, os.path.join(preview_dir, model.name + ".png"))
        for model in built:
            model.col.hide_render = False
            model.col.hide_viewport = False

    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(repo, "Tools", "blender", "models.blend"))
    return report


def _setup_preview_render():
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    shading = scene.display.shading
    shading.light = 'STUDIO'
    shading.color_type = 'OBJECT'
    shading.show_cavity = True
    shading.show_shadows = False
    scene.render.resolution_x = 512
    scene.render.resolution_y = 512
    if scene.world is not None:
        scene.world.color = (0.045, 0.047, 0.06)


def _render_preview(model, all_models, path):
    scene = bpy.context.scene
    for other in all_models:
        other.col.hide_render = other is not model

    lo = Vector((1e9, 1e9, 1e9))
    hi = Vector((-1e9, -1e9, -1e9))
    for obj in model.col.objects:
        for corner in obj.bound_box:
            p = obj.matrix_world @ Vector(corner)
            lo = Vector((min(lo.x, p.x), min(lo.y, p.y), min(lo.z, p.z)))
            hi = Vector((max(hi.x, p.x), max(hi.y, p.y), max(hi.z, p.z)))
    center = (lo + hi) * 0.5
    radius = max((hi - lo).length * 0.5, 0.2)

    cam = bpy.data.objects.get("CO_PreviewCam")
    if cam is None:
        cam = bpy.data.objects.new("CO_PreviewCam", bpy.data.cameras.new("CO_PreviewCam"))
        scene.collection.objects.link(cam)
    cam.data.lens = 70
    direction = Vector((0.5, 0.85, 0.75)).normalized()
    distance = radius / math.tan(math.atan(18.0 / 70.0)) * 1.15
    cam.location = center + direction * distance
    cam.rotation_euler = (center - cam.location).to_track_quat('-Z', 'Y').to_euler()
    scene.camera = cam
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
