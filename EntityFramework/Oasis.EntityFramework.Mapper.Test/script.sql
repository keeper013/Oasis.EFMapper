CREATE TABLE "ByteSourceEntity" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ByteSourceEntity" PRIMARY KEY AUTOINCREMENT,
    "Timestamp" INTEGER NULL,
    "SomeProperty" INTEGER NOT NULL
);


CREATE TABLE "CollectionEntity1" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_CollectionEntity1" PRIMARY KEY AUTOINCREMENT,
    "IntProp" INTEGER NOT NULL,
    "Timestamp" INTEGER NULL
);


CREATE TABLE "DerivedEntity1" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_DerivedEntity1" PRIMARY KEY AUTOINCREMENT,
    "StringProp" TEXT NULL,
    "Timestamp" INTEGER NULL,
    "IntProp" INTEGER NOT NULL
);


CREATE TABLE "DerivedEntity1_1" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_DerivedEntity1_1" PRIMARY KEY AUTOINCREMENT,
    "IntProp" INTEGER NOT NULL,
    "Timestamp" INTEGER NULL
);


CREATE TABLE "IntSourceEntity" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_IntSourceEntity" PRIMARY KEY AUTOINCREMENT,
    "Timestamp" INTEGER NULL,
    "SomeProperty" INTEGER NOT NULL
);


CREATE TABLE "ListEntity1" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ListEntity1" PRIMARY KEY AUTOINCREMENT,
    "IntProp" INTEGER NOT NULL,
    "Timestamp" INTEGER NULL
);


CREATE TABLE "ListEntity2" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ListEntity2" PRIMARY KEY AUTOINCREMENT,
    "IntProp" INTEGER NOT NULL,
    "Timestamp" INTEGER NULL
);


CREATE TABLE "ListIEntity1" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ListIEntity1" PRIMARY KEY AUTOINCREMENT,
    "IntProp" INTEGER NOT NULL,
    "Timestamp" INTEGER NULL
);


CREATE TABLE "LongSourceEntity" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_LongSourceEntity" PRIMARY KEY AUTOINCREMENT,
    "Timestamp" INTEGER NULL,
    "SomeProperty" INTEGER NOT NULL
);


CREATE TABLE "NByteSourceEntity" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_NByteSourceEntity" PRIMARY KEY AUTOINCREMENT,
    "Timestamp" INTEGER NULL,
    "SomeProperty" INTEGER NOT NULL
);


CREATE TABLE "NIntSourceEntity" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_NIntSourceEntity" PRIMARY KEY AUTOINCREMENT,
    "Timestamp" INTEGER NULL,
    "SomeProperty" INTEGER NOT NULL
);


CREATE TABLE "NLongSourceEntity" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_NLongSourceEntity" PRIMARY KEY AUTOINCREMENT,
    "Timestamp" INTEGER NULL,
    "SomeProperty" INTEGER NOT NULL
);


CREATE TABLE "NShortSourceEntity" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_NShortSourceEntity" PRIMARY KEY AUTOINCREMENT,
    "Timestamp" INTEGER NULL,
    "SomeProperty" INTEGER NOT NULL
);


CREATE TABLE "Outer1" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Outer1" PRIMARY KEY AUTOINCREMENT,
    "IntProp" INTEGER NOT NULL,
    "Timestamp" INTEGER NULL
);


CREATE TABLE "RecursiveEntity1" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_RecursiveEntity1" PRIMARY KEY AUTOINCREMENT,
    "StringProperty" TEXT NULL,
    "Parent_Id" INTEGER NULL,
    "Timestamp" INTEGER NULL,
    CONSTRAINT "FK_RecursiveEntity1_RecursiveEntity1_Parent_Id" FOREIGN KEY ("Parent_Id") REFERENCES "RecursiveEntity1" ("Id") ON DELETE SET NULL
);


CREATE TABLE "ScalarEntity1" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ScalarEntity1" PRIMARY KEY AUTOINCREMENT,
    "IntProp" INTEGER NOT NULL,
    "LongNullableProp" INTEGER NULL,
    "StringProp" TEXT NULL,
    "ByteArrayProp" BLOB NULL,
    "Timestamp" INTEGER NULL
);


CREATE TABLE "ShortSourceEntity" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ShortSourceEntity" PRIMARY KEY AUTOINCREMENT,
    "Timestamp" INTEGER NULL,
    "SomeProperty" INTEGER NOT NULL
);


CREATE TABLE "ScalarEntity1Item" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ScalarEntity1Item" PRIMARY KEY AUTOINCREMENT,
    "IntProp" INTEGER NOT NULL,
    "LongNullableProp" INTEGER NULL,
    "StringProp" TEXT NULL,
    "ByteArrayProp" BLOB NULL,
    "DerivedEntity1Id" INTEGER NOT NULL,
    "DerivedEntity1_1Id" INTEGER NOT NULL,
    "Timestamp" INTEGER NULL,
    CONSTRAINT "FK_ScalarEntity1Item_DerivedEntity1_1_DerivedEntity1_1Id" FOREIGN KEY ("DerivedEntity1_1Id") REFERENCES "DerivedEntity1_1" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ScalarEntity1Item_DerivedEntity1_DerivedEntity1Id" FOREIGN KEY ("DerivedEntity1Id") REFERENCES "DerivedEntity1" ("Id") ON DELETE CASCADE
);


CREATE TABLE "SubEntity2" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_SubEntity2" PRIMARY KEY AUTOINCREMENT,
    "StringProp" TEXT NULL,
    "ListEntityId" INTEGER NULL,
    "Timestamp" INTEGER NULL,
    CONSTRAINT "FK_SubEntity2_ListEntity2_ListEntityId" FOREIGN KEY ("ListEntityId") REFERENCES "ListEntity2" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "SubScalarEntity1" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_SubScalarEntity1" PRIMARY KEY AUTOINCREMENT,
    "CollectionEntityId" INTEGER NULL,
    "ListIEntityId" INTEGER NULL,
    "ListEntityId" INTEGER NULL,
    "IntProp" INTEGER NOT NULL,
    "LongNullableProp" INTEGER NULL,
    "StringProp" TEXT NULL,
    "ByteArrayProp" BLOB NULL,
    "Timestamp" INTEGER NULL,
    CONSTRAINT "FK_SubScalarEntity1_CollectionEntity1_CollectionEntityId" FOREIGN KEY ("CollectionEntityId") REFERENCES "CollectionEntity1" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_SubScalarEntity1_ListEntity1_ListEntityId" FOREIGN KEY ("ListEntityId") REFERENCES "ListEntity1" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_SubScalarEntity1_ListIEntity1_ListIEntityId" FOREIGN KEY ("ListIEntityId") REFERENCES "ListIEntity1" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "Inner1_1" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Inner1_1" PRIMARY KEY AUTOINCREMENT,
    "LongProp" INTEGER NOT NULL,
    "Outer_Id" INTEGER NULL,
    "Timestamp" INTEGER NULL,
    CONSTRAINT "FK_Inner1_1_Outer1_Outer_Id" FOREIGN KEY ("Outer_Id") REFERENCES "Outer1" ("Id") ON DELETE SET NULL
);


CREATE TABLE "Inner1_2" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Inner1_2" PRIMARY KEY AUTOINCREMENT,
    "StringProp" TEXT NULL,
    "Outer_Id" INTEGER NULL,
    "Timestamp" INTEGER NULL,
    CONSTRAINT "FK_Inner1_2_Outer1_Outer_Id" FOREIGN KEY ("Outer_Id") REFERENCES "Outer1" ("Id") ON DELETE SET NULL
);


CREATE UNIQUE INDEX "IX_Inner1_1_Outer_Id" ON "Inner1_1" ("Outer_Id");


CREATE UNIQUE INDEX "IX_Inner1_2_Outer_Id" ON "Inner1_2" ("Outer_Id");


CREATE UNIQUE INDEX "IX_RecursiveEntity1_Parent_Id" ON "RecursiveEntity1" ("Parent_Id");


CREATE INDEX "IX_ScalarEntity1Item_DerivedEntity1_1Id" ON "ScalarEntity1Item" ("DerivedEntity1_1Id");


CREATE INDEX "IX_ScalarEntity1Item_DerivedEntity1Id" ON "ScalarEntity1Item" ("DerivedEntity1Id");


CREATE INDEX "IX_SubEntity2_ListEntityId" ON "SubEntity2" ("ListEntityId");


CREATE INDEX "IX_SubScalarEntity1_CollectionEntityId" ON "SubScalarEntity1" ("CollectionEntityId");


CREATE INDEX "IX_SubScalarEntity1_ListEntityId" ON "SubScalarEntity1" ("ListEntityId");


CREATE INDEX "IX_SubScalarEntity1_ListIEntityId" ON "SubScalarEntity1" ("ListIEntityId");


