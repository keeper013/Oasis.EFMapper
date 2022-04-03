CREATE TABLE "Book" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Book" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NULL
);


CREATE TABLE "Borrower" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Borrower" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NULL
);


CREATE TABLE "BorrowRecord" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_BorrowRecord" PRIMARY KEY AUTOINCREMENT,
    "BorrowerId" INTEGER NOT NULL,
    "Book_Id" INTEGER NOT NULL,
    CONSTRAINT "FK_BorrowRecord_Book_Book_Id" FOREIGN KEY ("Book_Id") REFERENCES "Book" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_BorrowRecord_Borrower_BorrowerId" FOREIGN KEY ("BorrowerId") REFERENCES "Borrower" ("Id") ON DELETE RESTRICT
);


CREATE UNIQUE INDEX "IX_BorrowRecord_Book_Id" ON "BorrowRecord" ("Book_Id");


CREATE INDEX "IX_BorrowRecord_BorrowerId" ON "BorrowRecord" ("BorrowerId");


