import matplotlib.pyplot as plt
from matplotlib.figure import Axes
from mpl_toolkits.mplot3d.art3d import Poly3DCollection
import numpy as np

from .simulation import FieldNode
from .apsim import ApsimController

def plot_field_geo(ax: Axes, field: FieldNode, color="g"):
    """Add a the physical dimensions of a field as a subplot in a figure.

    Args:
        ax      (:obj:`Axes`)
        fig     (:obj:`Figure`)
        field   (:obj:`FieldNode`)
        color   (:obj:`Optional[str]`)  Field color.
    """
    [x_coord, y_coord, z_coord] = [float(coord) for coord in field.coords]
    r = float(field.radius)

    # Define the vertices.
    x_min = x_coord - r
    y_min = y_coord - r
    z_min = z_coord - r
    x_max = x_coord + r
    y_max = y_coord + r
    z_max = z_coord + r

    vertices = np.array([
        [x_min, y_min, z_min],
        [x_max, y_min, z_min],
        [x_max, y_max, z_min],
        [x_min, y_max, z_min],
        [x_min, y_min, z_max],
        [x_max, y_min, z_max],
        [x_max, y_max, z_max],
        [x_min, y_max, z_max]
    ])

    # Define the faces of the rectangular prism.
    faces = [
        [vertices[v] for v in [0, 1, 2, 3]],    # Bottom face.
        [vertices[v] for v in [4, 5, 6, 7]],    # Top face.
        [vertices[v] for v in [0, 1, 5, 4]],    # Front face.
        [vertices[v] for v in [2, 3, 7, 6]],    # Back face.
        [vertices[v] for v in [0, 3, 7, 4]],    # Left face.
        [vertices[v] for v in [1, 2, 6, 5]]     # Right face.
    ]

    rprism = Poly3DCollection(
        faces,
        facecolors=color,
        alpha=0.5,
        edgecolors="black",
        linewidths=1
    )
    ax.add_collection3d(rprism)

def get_sphere_coords(field: FieldNode, u, v):
    """Let's get parametric.
    """
    h2o_scaler = 1 
    center = [float(coord) for coord in field.coords]
    radius = h2o_scaler * float(field.v_water)
    x = center[0] + radius * np.sin(v) * np.cos(u)
    y = center[1] + radius * np.sin(v) * np.sin(u)
    z = center[2] + radius * np.cos(v)
    return x, y, z

def plot_field_h2o(ax: Axes, field: FieldNode, color="c"):
    """Add a field's water content as a spherical subplot in a figure.

    Args:
        ax      (:obj:`Axes`)
        field   (:obj:`FieldNode`)
    """
    [x_coord, y_coord, z_coord] = [float(coord) for coord in field.coords]
    r = float(field.v_water)
    u = np.linspace(0, 2 * np.pi, 100)
    v = np.linspace(0, np.pi, 100)
    u, v = np.meshgrid(u, v)
    x, y, z = get_sphere_coords(field, u, v)
    ax.plot_surface(
        x,
        y,
        z,
        color=color,
        alpha=0.2
    )

def plot_field(geox: Axes, h2ox: Axes, field: FieldNode):
    """Add a field as a subplot in a figure.

    Args:
        geox    (:obj:`Axes`)   Axes for geospatial depiction.
        h2ox    (:obj:`Axes`)   Axes for H2O volume depiction.
        field   (:obj:`FieldNode`)
    """
    plot_field_geo(geox, field)
    plot_field_h2o(h2ox, field)

def _format_geo_plot(
        ax: Axes,
        x: list[float],
        y: list[float],
        z: list[float],
        margin: float = 3.0
    ):
    """
    ax: (:obj:`Axes`)
    """
    ax.set_xlabel("X [acres]")
    ax.set_ylabel("Y [acres]")
    ax.set_zlabel("Z [acres]")
    ax.set_xlim([min(x) - margin, max(x) + margin])
    ax.set_ylim([min(y) - margin, max(y) + margin])
    ax.set_zlim([min(z) - margin, max(z) + margin])

def _format_h2o_plot(
        ax: Axes,
        x: list[float],
        y: list[float],
        z: list[float],
        margin: float = 3.0
    ):
    """
    ax: (:obj:`Axes`)
    """
    ax.set_xlabel("X [gal]")
    ax.set_ylabel("Y [gal]")
    ax.set_zlabel("Z [gal]")
    ax.set_xlim([min(x) - margin, max(x) + margin])
    ax.set_ylim([min(y) - margin, max(y) + margin])
    ax.set_zlim([min(z) - margin, max(z) + margin])

def plot_oasis(controller: ApsimController):
    """Generate a 3D plot representation of an OASIS simulation.

    Args:
        controller (:obj:`ApsimController`)
    """
    fig = plt.figure(figsize=(10, 8))
    geox = fig.add_subplot(211, projection="3d")
    h2ox = fig.add_subplot(212, projection="3d")
    x, y, z = [], [], []
    for field in controller.fields:
        plot_field(geox, h2ox, field)
        [x_coord, y_coord, z_coord] = field.coords
        x.append(float(x_coord))
        y.append(float(y_coord))
        z.append(float(z_coord))

    _format_geo_plot(geox, x, y, z)
    _format_h2o_plot(h2ox, x, y, z)

    geox.set_title(f"Sample Apsim Field Positions")
    h2ox.set_title(f"Sample Apsim Field Total H2O Volumes")
    plt.tight_layout()
    plt.show()
   
def plot_vwc_layer(ts_arr, vwc_arr):
    """Plots fields as columns with vwc of soil layers as rows"""
 
    shape = vwc_arr.shape
    vwc_arr = vwc_arr.reshape(shape[0], shape[1]*shape[2], shape[3])
    
    fields = vwc_arr.shape[1]
    layers = vwc_arr.shape[2]

    fig, axs = plt.subplots(layers, fields, figsize=(12, 8))
    
    for i in range(fields):
        axs[0,i].set_title(f"Field{i}")
        for j in range(layers):
            if i == 0:
                axs[j,0].set_ylabel(f"Layer{j}")
            ax = axs[j,i]
            ax.plot(ts_arr, vwc_arr[:,i,j])
            ax.set_title(f"Field ({i},{j})")
            ax.grid()

    axs[0,0].legend()

    plt.tight_layout()
    plt.show() 
    
def plot_vwc_field_grid(ts_arr, vwc_arr):
    """Plots the vwc of each field in a grid""" 
    
    rows = vwc_arr.shape[1]
    cols = vwc_arr.shape[2]

    fig, axs = plt.subplots(rows, cols, figsize=(12, 8))
    
    for i in range(rows):
        for j in range(cols):
            ax = axs[i,j]
            ax.plot(ts_arr, vwc_arr[:,i,j,:])
            ax.set_title(f"Field ({i},{j})")
            ax.grid()

    axs[0,0].legend()

    plt.tight_layout()
    plt.show()
    