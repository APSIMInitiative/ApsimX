class FieldNode(object):
    """A single Field, corresponding to a node in the OASIS sim.
    
    Create a new Field that is linked to its instance in APSIM.
    
    Attributes:
        id (int): Location (index) within Apsim list.
        info (dict): Includes the following keys:
            { "X", "Y", "Z", "Radius", "WaterVolume", "Name" }
        
    """
    def __init__(
            self,
            server,
            configs: dict = {}
        ):
        """
        Args:
            configs (:obj: `dict`, optional): Pre-formatted field configs; see
                [example_url.com] for supported configurations.

        TODO:
            * Replace XYZ with GPS data and elevation/depth?
        """
        self.id = None
        self.info = {}
        for key in ["Name", "SW", "X", "Y", "Z"]:
            self.info[key] = configs[key]
        # TODO(nubby): Make the radius/area settings better.
        self.info["Area"] = str((float(configs["Radius"]) * 2)**2)

        self.coords = [configs["X"], configs["Y"], configs["Z"]]
        self.radius = configs["Radius"]
        self.name = configs["Name"]
        self.v_water = configs["SW"]

        # Aliases.
        self.socket = server.socket
        self.send_command = server.send_command
        self.create()

    def __repr__(self):
        return "{}: {} acres, {} gal H2O; @({},{},{})".format(
            self.info["Name"],
            self.info["SW"],
            self.info["Area"],
            self.info["X"],
            self.info["Y"],
            self.info["Z"]
        )


    def digest_configs(
            self,
            fpath: str
        ):
        """Import configurations from a CSV file.
        Args:
            fpath (str): Relative path of file.
        """
        pass

    def _format_configs(self):
        """Prepare FieldNode configs for creating a new Field in Apsim.
        Returns:
            csv_configs (:obj:`list` of :obj:`str`): List of comma-separated
                key-value pairs for each configuration provided.
        """
        return ["{},{}".format(key, val) for key, val in self.info.items()]

    def create(self):
        """Create a new field and link with ID reference returned by Apsim."""
        csv_configs = self._format_configs()
        self.id = int.from_bytes(
            self.send_command(
                command="field",
                args=csv_configs,
                unpack=False
            ),
            "big",
            signed=False
        )