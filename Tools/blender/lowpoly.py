import json
import math

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

LIMB = {None: 0.0, "body": 0.0, "leg": 1.0, "arm": 2.0, "held": 3.0}


def _reset_collection(name):
    col = bpy.data.collections.get(name)
    if col is not None:
        for obj in list(col.objects):
            mesh = obj.data
            bpy.data.objects.remove(obj, do_unlink=True)
            if mesh is not None and mesh.users == 0:
                bpy.data.meshes.remove(mesh)
        bpy.data.collections.remove(col)
    col = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(col)
    return col


class Model:
    def __init__(self, name):
        self.name = name
        self.col = _reset_collection("CO_" + name)
        self._rgb = (255, 255, 255)
        self._emission = 0.0
        self._limb = None
        self._side = 0.0
        self._pivot = (0.0, 0.0, 0.0)


    def paint(self, rgb, emission=0.0):
        self._rgb = tuple(rgb)
        self._emission = emission
        return self

    def limb(self, kind=None, side=0.0, pivot=(0.0, 0.0, 0.0)):
        self._limb = kind
        self._side = side
        self._pivot = tuple(pivot)
        return self


    def box(self, name, size, loc, rot=(0, 0, 0), taper=(1.0, 1.0), top_shift=(0.0, 0.0), bevel=0.0):
        bm = bmesh.new()
        bmesh.ops.create_cube(bm, size=1.0)
        sx, sy, sz = size
        for v in bm.verts:
            top = v.co.z > 0
            v.co.x *= sx * (taper[0] if top else 1.0)
            v.co.y *= sy * (taper[1] if top else 1.0)
            v.co.z *= sz
            if top:
                v.co.x += top_shift[0]
                v.co.y += top_shift[1]
        return self._finish(bm, name, loc, rot, bevel)

    def prism(self, name, radius, height, loc, rot=(0, 0, 0), sides=6, top_radius=None, bevel=0.0, spin=0.0):
        bm = bmesh.new()
        r2 = radius if top_radius is None else top_radius
        bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=sides,
                              radius1=radius, radius2=max(r2, 0.0), depth=height,
                              matrix=Matrix.Rotation(spin, 4, 'Z'))
        bmesh.ops.remove_doubles(bm, verts=bm.verts[:], dist=0.0005)
        for v in bm.verts:
            v.co.z += height * 0.5
        return self._finish(bm, name, loc, rot, bevel)

    def gem(self, name, radius, height, loc, rot=(0, 0, 0), sides=6, waist=0.35, spin=0.0):
        bm = bmesh.new()
        ring = []
        for i in range(sides):
            a = spin + i * 2 * math.pi / sides
            ring.append(bm.verts.new((math.cos(a) * radius, math.sin(a) * radius, height * waist)))
        bottom = bm.verts.new((0, 0, 0))
        top = bm.verts.new((0, 0, height))
        for i in range(sides):
            a, b = ring[i], ring[(i + 1) % sides]
            bm.faces.new((a, b, top))
            bm.faces.new((b, a, bottom))
        return self._finish(bm, name, loc, rot, 0.0)

    def poly(self, name, verts, faces, loc=(0, 0, 0), rot=(0, 0, 0), bevel=0.0):
        bm = bmesh.new()
        bverts = [bm.verts.new(v) for v in verts]
        for f in faces:
            bm.faces.new([bverts[i] for i in f])
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        return self._finish(bm, name, loc, rot, bevel)

    def strut(self, name, start, end, size, bevel=0.0, taper=(1.0, 1.0)):
        a, b = Vector(start), Vector(end)
        direction = b - a
        length = direction.length
        bm = bmesh.new()
        bmesh.ops.create_cube(bm, size=1.0)
        for v in bm.verts:
            top = v.co.z > 0
            v.co.x *= size[0] * (taper[0] if top else 1.0)
            v.co.y *= size[1] * (taper[1] if top else 1.0)
            v.co.z = (v.co.z + 0.5) * length
        quat = direction.to_track_quat('Z', 'Y')
        return self._finish(bm, name, a, quat.to_euler(), bevel)

    def _finish(self, bm, name, loc, rot, bevel):
        if bevel > 0:
            bmesh.ops.bevel(bm, geom=bm.edges[:], offset=bevel, segments=1, profile=0.5, affect='EDGES')
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
        mesh = bpy.data.meshes.new(f"{self.name}_{name}")
        bm.to_mesh(mesh)
        bm.free()

        obj = bpy.data.objects.new(f"{self.name}_{name}", mesh)
        self.col.objects.link(obj)
        obj.location = Vector(loc)
        obj.rotation_euler = rot if isinstance(rot, Euler) else Euler(rot)

        obj["rgb"] = list(self._rgb)
        obj["emission"] = float(self._emission)
        obj["limb"] = LIMB[self._limb]
        obj["side"] = float(self._side)
        obj["pivot"] = list(self._pivot)
        boost = 1.0 + self._emission * 0.6
        obj.color = (min(1.0, self._rgb[0] / 255 * boost), min(1.0, self._rgb[1] / 255 * boost),
                     min(1.0, self._rgb[2] / 255 * boost), 1.0)
        return obj


    def export(self, path):
        deps = bpy.context.evaluated_depsgraph_get()
        deps.update()
        positions, normals, colors, limbs, pivots, triangles = [], [], [], [], [], []

        for obj in self.col.objects:
            if obj.type != 'MESH':
                continue
            evaluated = obj.evaluated_get(deps)
            mesh = evaluated.to_mesh()
            mesh.calc_loop_triangles()
            world = obj.matrix_world
            normal_matrix = world.to_3x3().inverted().transposed()
            rgb = [c / 255.0 for c in obj["rgb"]]
            emission = float(obj["emission"])
            limb = float(obj["limb"])
            side = float(obj["side"])
            pivot = obj["pivot"]

            for tri in mesh.loop_triangles:
                n = (normal_matrix @ mesh.polygons[tri.polygon_index].normal).normalized()
                base = len(positions) // 3
                for vi in tri.vertices:
                    p = world @ mesh.vertices[vi].co
                    positions += [round(p.x, 4), round(p.z, 4), round(p.y, 4)]
                    normals += [round(n.x, 4), round(n.z, 4), round(n.y, 4)]
                    colors += [round(rgb[0], 4), round(rgb[1], 4), round(rgb[2], 4), round(emission, 3)]
                    limbs += [limb, side]
                    pivots += [round(pivot[0], 4), round(pivot[2], 4), round(pivot[1], 4)]
                triangles += [base, base + 2, base + 1]
            evaluated.to_mesh_clear()

        data = {"name": self.name, "positions": positions, "normals": normals, "colors": colors,
                "limbs": limbs, "pivots": pivots, "triangles": triangles}
        with open(path, "w", encoding="utf-8") as f:
            json.dump(data, f, separators=(",", ":"))
        return len(triangles) // 3


def frame_view(model_collection_name, distance_scale=1.0, view=(1.05, 0.0, 0.6)):
    for area in bpy.context.screen.areas:
        if area.type != 'VIEW_3D':
            continue
        space = area.spaces.active
        space.shading.type = 'SOLID'
        space.shading.color_type = 'OBJECT'
        space.shading.light = 'STUDIO'
        space.shading.show_cavity = False
        space.shading.background_type = 'VIEWPORT'
        space.shading.background_color = (0.16, 0.17, 0.2)
        space.overlay.show_overlays = False
        region3d = space.region_3d
        region3d.view_perspective = 'PERSP'
        region3d.view_rotation = Euler(view).to_quaternion()

        col = bpy.data.collections.get(model_collection_name)
        if col is None:
            return
        lo = Vector((1e9, 1e9, 1e9))
        hi = Vector((-1e9, -1e9, -1e9))
        for obj in col.objects:
            for corner in obj.bound_box:
                p = obj.matrix_world @ Vector(corner)
                lo = Vector((min(lo.x, p.x), min(lo.y, p.y), min(lo.z, p.z)))
                hi = Vector((max(hi.x, p.x), max(hi.y, p.y), max(hi.z, p.z)))
        region3d.view_location = (lo + hi) * 0.5
        region3d.view_distance = (hi - lo).length * 1.25 * distance_scale
