CREATE TABLE "ByteSourceEntity" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ByteSourceEntity" PRIMARY KEY AUTOINCREMENT,
    "ConcurrencyToken" BLOB NOT NULL,
    "SomeProperty" INTEGER NOT NULL
);


CREATE TABLE "CollectionEntity1" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_CollectionEntity1" PRIMARY KEY AUTOINCREMENT,
    "IntProp" INTEGER NOT NULL,
    "ConcurrencyToken" BLOB NOT NULL
);


CREATE TABLE "DerivedEntity1" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_DerivedEntity1" PRIMARY KEY AUTOINCREMENT,
    "StringProp" TEXT NULL,
    "ConcurrencyToken" BLOB NOT NULL,
    "IntProp" INTEGER NOT NULL
);


CREATE TABLE "DerivedEntity1_1" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_DerivedEntity1_1" PRIMARY KEY AUTOINCREMENT,
    "IntProp" INTEGER NOT NULL,
    "ConcurrencyToken" BLOB NOT NULL
);


CREATE TABLE "EnumEntity1" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_EnumEntity1" PRIMARY KEY AUTOINCREMENT,
    "EnumProperty" INTEGER NOT NULL,
    "ConcurrencyToken" BLOB NOT NULL
);


CREATE TABLE "IntSourceEntity" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_IntSourceEntity" PRIMARY KEY AUTOINCREMENT,
    "ConcurrencyToken" BLOB NOT NULL,
    "SomeProperty" INTEGER NOT NULL
);


CREATE TABLE "ListEntity1" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ListEntity1" PRIMARY KEY AUTOINCREMENT,
    "IntProp" INTEGER NOT NULL,
    "ConcurrencyToken" BLOB NOT NULL
);


CREATE TABLE "ListEntity2" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ListEntity2" PRIMARY KEY AUTOINCREMENT,
    "IntProp" INTEGER NOT NULL,
    "ConcurrencyToken" BLOB NOT NULL
);


CREATE TABLE "ListIEntity1" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ListIEntity1" PRIMARY KEY AUTOINCREMENT,
    "IntProp" INTEGER NOT NULL,
    "ConcurrencyToken" BLOB NOT NULL
);


CREATE TABLE "LongSourceEntity" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_LongSourceEntity" PRIMARY KEY AUTOINCREMENT,
    "ConcurrencyToken" BLOB NOT NULL,
    "SomeProperty" INTEGER NOT NULL
);


CREATE TABLE "ManyToManyChild2" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ManyToManyChild2" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "ConcurrencyToken" BLOB NOT NULL
);


CREATE TABLE "ManyToManyParent2" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ManyToManyParent2" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "ConcurrencyToken" BLOB NOT NULL
);


CREATE TABLE "NByteSourceEntity" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_NByteSourceEntity" PRIMARY KEY AUTOINCREMENT,
    "ConcurrencyToken" BLOB NOT NULL,
    "SomeProperty" INTEGER NOT NULL
);


CREATE TABLE "NIntSourceEntity" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_NIntSourceEntity" PRIMARY KEY AUTOINCREMENT,
    "ConcurrencyToken" BLOB NOT NULL,
    "SomeProperty" INTEGER NOT NULL
);


CREATE TABLE "NLongSourceEntity" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_NLongSourceEntity" PRIMARY KEY AUTOINCREMENT,
    "ConcurrencyToken" BLOB NOT NULL,
    "SomeProperty" INTEGER NOT NULL
);


CREATE TABLE "NShortSourceEntity" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_NShortSourceEntity" PRIMARY KEY AUTOINCREMENT,
    "ConcurrencyToken" BLOB NOT NULL,
    "SomeProperty" INTEGER NOT NULL
);


CREATE TABLE "NullableEnumEntity1" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_NullableEnumEntity1" PRIMARY KEY AUTOINCREMENT,
    "EnumProperty" INTEGER NULL,
    "ConcurrencyToken" BLOB NOT NULL
);


CREATE TABLE "PrincipalOptional1" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_PrincipalOptional1" PRIMARY KEY AUTOINCREMENT,
    "IntProp" INTEGER NOT NULL,
    "ConcurrencyToken" BLOB NOT NULL
);


CREATE TABLE "PrincipalRequired1" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_PrincipalRequired1" PRIMARY KEY AUTOINCREMENT,
    "IntProp" INTEGER NOT NULL,
    "ConcurrencyToken" BLOB NOT NULL
);


CREATE TABLE "RecursiveEntity1" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_RecursiveEntity1" PRIMARY KEY AUTOINCREMENT,
    "StringProperty" TEXT NULL,
    "Parent_Id" INTEGER NULL,
    "ConcurrencyToken" BLOB NOT NULL,
    CONSTRAINT "FK_RecursiveEntity1_RecursiveEntity1_Parent_Id" FOREIGN KEY ("Parent_Id") REFERENCES "RecursiveEntity1" ("Id") ON DELETE SET NULL
);


CREATE TABLE "ScalarEntity1" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ScalarEntity1" PRIMARY KEY AUTOINCREMENT,
    "IntProp" INTEGER NOT NULL,
    "LongNullableProp" INTEGER NULL,
    "StringProp" TEXT NULL,
    "ByteArrayProp" BLOB NULL,
    "ConcurrencyToken" BLOB NOT NULL
);


CREATE TABLE "SessionTestingList1_1" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_SessionTestingList1_1" PRIMARY KEY AUTOINCREMENT,
    "ConcurrencyToken" BLOB NOT NULL
);


CREATE TABLE "SessionTestingList1_2" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_SessionTestingList1_2" PRIMARY KEY AUTOINCREMENT,
    "ConcurrencyToken" BLOB NOT NULL
);


CREATE TABLE "ShortSourceEntity" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ShortSourceEntity" PRIMARY KEY AUTOINCREMENT,
    "ConcurrencyToken" BLOB NOT NULL,
    "SomeProperty" INTEGER NOT NULL
);


CREATE TABLE "ToDatabaseEntity1" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ToDatabaseEntity1" PRIMARY KEY AUTOINCREMENT,
    "IntProperty" INTEGER NOT NULL,
    "ConcurrencyToken" BLOB NOT NULL
);


CREATE TABLE "UnmatchedPrincipal1" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_UnmatchedPrincipal1" PRIMARY KEY AUTOINCREMENT,
    "ConcurrencyToken" BLOB NOT NULL
);


CREATE TABLE "ScalarEntity1Item" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ScalarEntity1Item" PRIMARY KEY AUTOINCREMENT,
    "IntProp" INTEGER NOT NULL,
    "LongNullableProp" INTEGER NULL,
    "StringProp" TEXT NULL,
    "ByteArrayProp" BLOB NULL,
    "DerivedEntity1Id" INTEGER NOT NULL,
    "DerivedEntity1_1Id" INTEGER NOT NULL,
    "ConcurrencyToken" BLOB NOT NULL,
    CONSTRAINT "FK_ScalarEntity1Item_DerivedEntity1_1_DerivedEntity1_1Id" FOREIGN KEY ("DerivedEntity1_1Id") REFERENCES "DerivedEntity1_1" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ScalarEntity1Item_DerivedEntity1_DerivedEntity1Id" FOREIGN KEY ("DerivedEntity1Id") REFERENCES "DerivedEntity1" ("Id") ON DELETE CASCADE
);


CREATE TABLE "SubEntity2" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_SubEntity2" PRIMARY KEY AUTOINCREMENT,
    "StringProp" TEXT NULL,
    "ListEntityId" INTEGER NULL,
    "ConcurrencyToken" BLOB NOT NULL,
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
    "ConcurrencyToken" BLOB NOT NULL,
    CONSTRAINT "FK_SubScalarEntity1_CollectionEntity1_CollectionEntityId" FOREIGN KEY ("CollectionEntityId") REFERENCES "CollectionEntity1" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_SubScalarEntity1_ListEntity1_ListEntityId" FOREIGN KEY ("ListEntityId") REFERENCES "ListEntity1" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_SubScalarEntity1_ListIEntity1_ListIEntityId" FOREIGN KEY ("ListIEntityId") REFERENCES "ListIEntity1" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "ManyToManyChild2ManyToManyParent2" (
    "ChildrenId" INTEGER NOT NULL,
    "ParentsId" INTEGER NOT NULL,
    CONSTRAINT "PK_ManyToManyChild2ManyToManyParent2" PRIMARY KEY ("ChildrenId", "ParentsId"),
    CONSTRAINT "FK_ManyToManyChild2ManyToManyParent2_ManyToManyChild2_ChildrenId" FOREIGN KEY ("ChildrenId") REFERENCES "ManyToManyChild2" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ManyToManyChild2ManyToManyParent2_ManyToManyParent2_ParentsId" FOREIGN KEY ("ParentsId") REFERENCES "ManyToManyParent2" ("Id") ON DELETE CASCADE
);


CREATE TABLE "DependentOptional1_1" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_DependentOptional1_1" PRIMARY KEY AUTOINCREMENT,
    "LongProp" INTEGER NOT NULL,
    "Outer_Id" INTEGER NULL,
    "ConcurrencyToken" BLOB NOT NULL,
    CONSTRAINT "FK_DependentOptional1_1_PrincipalOptional1_Outer_Id" FOREIGN KEY ("Outer_Id") REFERENCES "PrincipalOptional1" ("Id") ON DELETE SET NULL
);


CREATE TABLE "DependentOptional1_2" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_DependentOptional1_2" PRIMARY KEY AUTOINCREMENT,
    "StringProp" TEXT NULL,
    "Outer_Id" INTEGER NULL,
    "ConcurrencyToken" BLOB NOT NULL,
    CONSTRAINT "FK_DependentOptional1_2_PrincipalOptional1_Outer_Id" FOREIGN KEY ("Outer_Id") REFERENCES "PrincipalOptional1" ("Id") ON DELETE SET NULL
);


CREATE TABLE "DependentRequired1_1" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_DependentRequired1_1" PRIMARY KEY AUTOINCREMENT,
    "LongProp" INTEGER NOT NULL,
    "OuterId" INTEGER NOT NULL,
    "ConcurrencyToken" BLOB NOT NULL,
    CONSTRAINT "FK_DependentRequired1_1_PrincipalRequired1_OuterId" FOREIGN KEY ("OuterId") REFERENCES "PrincipalRequired1" ("Id") ON DELETE CASCADE
);


CREATE TABLE "DependentRequired1_2" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_DependentRequired1_2" PRIMARY KEY AUTOINCREMENT,
    "StringProp" TEXT NULL,
    "OuterId" INTEGER NOT NULL,
    "ConcurrencyToken" BLOB NOT NULL,
    CONSTRAINT "FK_DependentRequired1_2_PrincipalRequired1_OuterId" FOREIGN KEY ("OuterId") REFERENCES "PrincipalRequired1" ("Id") ON DELETE CASCADE
);


CREATE TABLE "ScalarItem1" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ScalarItem1" PRIMARY KEY AUTOINCREMENT,
    "StringProp" TEXT NULL,
    "List1Id" INTEGER NULL,
    "List2Id" INTEGER NULL,
    "ConcurrencyToken" BLOB NOT NULL,
    CONSTRAINT "FK_ScalarItem1_SessionTestingList1_1_List1Id" FOREIGN KEY ("List1Id") REFERENCES "SessionTestingList1_1" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_ScalarItem1_SessionTestingList1_2_List2Id" FOREIGN KEY ("List2Id") REFERENCES "SessionTestingList1_2" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "UnmatchedDependent1" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_UnmatchedDependent1" PRIMARY KEY AUTOINCREMENT,
    "PrincipalId" INTEGER NULL,
    "IntProp" INTEGER NOT NULL,
    "ConcurrencyToken" BLOB NOT NULL,
    CONSTRAINT "FK_UnmatchedDependent1_UnmatchedPrincipal1_PrincipalId" FOREIGN KEY ("PrincipalId") REFERENCES "UnmatchedPrincipal1" ("Id")
);


CREATE UNIQUE INDEX "IX_DependentOptional1_1_Outer_Id" ON "DependentOptional1_1" ("Outer_Id");


CREATE UNIQUE INDEX "IX_DependentOptional1_2_Outer_Id" ON "DependentOptional1_2" ("Outer_Id");


CREATE UNIQUE INDEX "IX_DependentRequired1_1_OuterId" ON "DependentRequired1_1" ("OuterId");


CREATE UNIQUE INDEX "IX_DependentRequired1_2_OuterId" ON "DependentRequired1_2" ("OuterId");


CREATE INDEX "IX_ManyToManyChild2ManyToManyParent2_ParentsId" ON "ManyToManyChild2ManyToManyParent2" ("ParentsId");


CREATE UNIQUE INDEX "IX_RecursiveEntity1_Parent_Id" ON "RecursiveEntity1" ("Parent_Id");


CREATE INDEX "IX_ScalarEntity1Item_DerivedEntity1_1Id" ON "ScalarEntity1Item" ("DerivedEntity1_1Id");


CREATE INDEX "IX_ScalarEntity1Item_DerivedEntity1Id" ON "ScalarEntity1Item" ("DerivedEntity1Id");


CREATE INDEX "IX_ScalarItem1_List1Id" ON "ScalarItem1" ("List1Id");


CREATE INDEX "IX_ScalarItem1_List2Id" ON "ScalarItem1" ("List2Id");


CREATE INDEX "IX_SubEntity2_ListEntityId" ON "SubEntity2" ("ListEntityId");


CREATE INDEX "IX_SubScalarEntity1_CollectionEntityId" ON "SubScalarEntity1" ("CollectionEntityId");


CREATE INDEX "IX_SubScalarEntity1_ListEntityId" ON "SubScalarEntity1" ("ListEntityId");


CREATE INDEX "IX_SubScalarEntity1_ListIEntityId" ON "SubScalarEntity1" ("ListIEntityId");


CREATE INDEX "IX_UnmatchedDependent1_PrincipalId" ON "UnmatchedDependent1" ("PrincipalId");


