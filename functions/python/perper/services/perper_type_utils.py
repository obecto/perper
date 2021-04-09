class PerperTypeUtils:

    # TODO: python has fewer basic types than java, direct conversion may not always be suitable
    java_type_names = {
        bool: "java.lang.Boolean",
        bytes: "java.lang.Byte",
        str: "java.lang.String",
        float: "java.lang.Float",
        int: "java.lang.Integer",
    }

    @classmethod
    def get_java_type_name(cls, _type):
        array_nesting = 0
        # TODO: For nested lists, add check if all underlying lists contain elements of the same type
        while isinstance(_type, list):
            if list in [type(el) for el in _type]:
                _type = _type[0]
                array_nesting += 1
            else:
                _type = type(_type[0])

        result = cls.java_type_names.get(_type, " ")
        if result == " ":
            return None
        elif array_nesting == 0:
            return result
        else:
            return "[" * array_nesting + f"L{result}"
        # return result if array_nesting == 0 else return "[" * array_nesting + f"L{result}"
