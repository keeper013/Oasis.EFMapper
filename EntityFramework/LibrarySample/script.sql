CREATE TABLE "Book" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Book" PRIMARY KEY AUTOINCREMENT,
    "ConcurrencyToken" INTEGER NOT NULL,
    "Name" TEXT NOT NULL
);


CREATE TABLE "Borrower" (
    "IdentityNumber" TEXT NOT NULL CONSTRAINT "PK_Borrower" PRIMARY KEY,
    "ConcurrencyToken" INTEGER NOT NULL,
    "Name" TEXT NOT NULL
);


CREATE TABLE "Tag" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Tag" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL
);


CREATE TABLE "Contact" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Contact" PRIMARY KEY AUTOINCREMENT,
    "ConcurrencyToken" INTEGER NOT NULL,
    "Borrower_IdentityNumber" TEXT NOT NULL,
    "PhoneNumber" TEXT NOT NULL,
    "Address" TEXT NULL,
    CONSTRAINT "FK_Contact_Borrower_Borrower_IdentityNumber" FOREIGN KEY ("Borrower_IdentityNumber") REFERENCES "Borrower" ("IdentityNumber") ON DELETE CASCADE
);


CREATE TABLE "Copy" (
    "Number" TEXT NOT NULL CONSTRAINT "PK_Copy" PRIMARY KEY,
    "ConcurrencyToken" INTEGER NOT NULL,
    "Reserver_IdentityNumber" TEXT NULL,
    "Borrower" TEXT NULL,
    "BookId" INTEGER NOT NULL,
    CONSTRAINT "FK_Copy_Book_BookId" FOREIGN KEY ("BookId") REFERENCES "Book" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Copy_Borrower_Borrower" FOREIGN KEY ("Borrower") REFERENCES "Borrower" ("IdentityNumber") ON DELETE RESTRICT,
    CONSTRAINT "FK_Copy_Borrower_Reserver_IdentityNumber" FOREIGN KEY ("Reserver_IdentityNumber") REFERENCES "Borrower" ("IdentityNumber") ON DELETE SET NULL
);


CREATE TABLE "BookTag" (
    "BooksId" INTEGER NOT NULL,
    "TagsId" INTEGER NOT NULL,
    CONSTRAINT "PK_BookTag" PRIMARY KEY ("BooksId", "TagsId"),
    CONSTRAINT "FK_BookTag_Book_BooksId" FOREIGN KEY ("BooksId") REFERENCES "Book" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_BookTag_Tag_TagsId" FOREIGN KEY ("TagsId") REFERENCES "Tag" ("Id") ON DELETE CASCADE
);


CREATE INDEX "IX_BookTag_TagsId" ON "BookTag" ("TagsId");


CREATE UNIQUE INDEX "IX_Contact_Borrower_IdentityNumber" ON "Contact" ("Borrower_IdentityNumber");


CREATE INDEX "IX_Copy_BookId" ON "Copy" ("BookId");


CREATE INDEX "IX_Copy_Borrower" ON "Copy" ("Borrower");


CREATE UNIQUE INDEX "IX_Copy_Reserver_IdentityNumber" ON "Copy" ("Reserver_IdentityNumber");


CREATE UNIQUE INDEX "IX_Tag_Name" ON "Tag" ("Name");


