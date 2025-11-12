INSERT INTO machines (id, name, type, pages_per_min, is_up)
VALUES
    (uuid_generate_v4(), 'Printer A', 'printer', 300, true),
    (uuid_generate_v4(), 'Coverer A', 'cover', NULL, true),
    (uuid_generate_v4(), 'Binder A', 'binder', NULL, true),
    (uuid_generate_v4(), 'Packager A', 'packager', NULL, true);

INSERT INTO books (id, title, copies, pages)
VALUES (uuid_generate_v4(), 'Deep C# Printing', 10, 120);
