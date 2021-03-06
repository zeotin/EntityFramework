// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Relational.Migrations.Operations;
using Microsoft.Data.Entity.Utilities;

namespace Microsoft.Data.Entity.Relational.Migrations.Sql
{
    // TODO: Review for SQL Server specific code
    public abstract class MigrationSqlGenerator : IMigrationSqlGenerator
    {
        private readonly ISqlGenerator _sql;

        public MigrationSqlGenerator([NotNull] ISqlGenerator sqlGenerator)
        {
            Check.NotNull(sqlGenerator, nameof(sqlGenerator));

            _sql = sqlGenerator;
        }

        public virtual IReadOnlyList<SqlBatch> Generate(
            IReadOnlyList<MigrationOperation> operations,
            IModel model = null)
        {
            Check.NotNull(operations, nameof(operations));

            var builder = new SqlBatchBuilder();
            foreach (var operation in operations)
            {
                // TODO: Too magic?
                ((dynamic)this).Generate((dynamic)operation, model, builder);
                builder.AppendLine();
            }

            builder.EndBatch();

            return builder.SqlBatches;
        }

        protected virtual void Generate(
            [NotNull] MigrationOperation operation,
            [CanBeNull] IModel model,
            [NotNull] SqlBatchBuilder batchBuilder)
        {
            throw new InvalidOperationException(Strings.UnknownOperation(GetType().Name, operation.GetType().Name));
        }

        protected virtual void Generate(
            [NotNull] CreateSequenceOperation operation,
            [CanBeNull] IModel model,
            [NotNull] SqlBatchBuilder builder)
        {
            Check.NotNull(operation, nameof(operation));
            Check.NotNull(builder, nameof(builder));

            builder
                .Append("CREATE SEQUENCE ")
                .Append(_sql.DelimitIdentifier(operation.Name, operation.Schema))
                .Append(" AS ")
                .Append(operation.StoreType)
                .Append(" START WITH ")
                .Append(operation.StartValue)
                .Append(" INCREMENT BY ")
                .Append(operation.IncrementBy);

            if (operation.MinValue.HasValue)
            {
                builder
                    .Append(" MINVALUE ")
                    .Append(operation.MinValue.Value);
            }

            if (operation.MaxValue.HasValue)
            {
                builder
                    .Append(" MAXVALUE ")
                    .Append(operation.MaxValue.Value);
            }

            builder.Append(";");
        }

        protected virtual void Generate(
            [NotNull] DropSequenceOperation operation,
            [CanBeNull] IModel model,
            [NotNull] SqlBatchBuilder builder)
        {
            Check.NotNull(operation, nameof(operation));
            Check.NotNull(builder, nameof(builder));

            builder
                .Append("DROP SEQUENCE ")
                .Append(_sql.DelimitIdentifier(operation.Name, operation.Schema))
                .Append(";");
        }

        protected abstract void Generate(
            [NotNull] RenameSequenceOperation operation,
            [CanBeNull] IModel model,
            [NotNull] SqlBatchBuilder builder);

        protected abstract void Generate(
            [NotNull] MoveSequenceOperation operation,
            [CanBeNull] IModel model,
            [NotNull] SqlBatchBuilder builder);

        protected virtual void Generate(
            [NotNull] AlterSequenceOperation operation,
            [CanBeNull] IModel model,
            [NotNull] SqlBatchBuilder builder)
        {
            Check.NotNull(operation, nameof(operation));
            Check.NotNull(builder, nameof(builder));

            builder
                .Append("ALTER SEQUENCE ")
                .Append(_sql.DelimitIdentifier(operation.Name, operation.Schema))
                .Append(" INCREMENT BY ")
                .Append(operation.IncrementBy);

            if (operation.MinValue.HasValue)
            {
                builder
                    .Append(" MINVALUE ")
                    .Append(operation.MinValue.Value);
            }
            else
            {
                builder.Append(" NO MINVALUE");
            }

            if (operation.MaxValue.HasValue)
            {
                builder
                    .Append(" MAXVALUE ")
                    .Append(operation.MaxValue.Value);
            }
            else
            {
                builder.Append(" NO MAXVALUE");
            }

            builder.Append(";");
        }

        protected virtual void Generate(
            [NotNull] CreateTableOperation operation,
            [CanBeNull] IModel model,
            [NotNull] SqlBatchBuilder builder)
        {
            Check.NotNull(operation, nameof(operation));
            Check.NotNull(builder, nameof(builder));

            builder
                .Append("CREATE TABLE ")
                .Append(_sql.DelimitIdentifier(operation.Name, operation.Schema))
                .AppendLine(" (");

            using (builder.Indent())
            {
                GenerateColumns(operation.Columns, builder);

                GenerateTableConstraints(operation, builder);
            }

            builder
                .AppendLine()
                .Append(");");
        }

        protected virtual void GenerateTableConstraints(
            [NotNull] CreateTableOperation operation,
            [NotNull] SqlBatchBuilder builder)
        {
            Check.NotNull(operation, nameof(operation));
            Check.NotNull(builder, nameof(builder));

            if (operation.PrimaryKey != null)
            {
                builder.AppendLine(",");

                GeneratePrimaryKey(operation.PrimaryKey, builder);
            }

            foreach (var uniqueConstraint in operation.UniqueConstraints)
            {
                builder.AppendLine(",");

                GenerateUniqueConstraint(uniqueConstraint, builder);
            }

            foreach (var foreignKey in operation.ForeignKeys)
            {
                builder.AppendLine(",");

                GenerateForeignKey(foreignKey, builder);
            }
        }

        protected virtual void Generate(
            [NotNull] DropTableOperation operation,
            [CanBeNull] IModel model,
            [NotNull] SqlBatchBuilder builder)
        {
            Check.NotNull(operation, nameof(operation));
            Check.NotNull(builder, nameof(builder));

            builder
                .Append("DROP TABLE ")
                .Append(_sql.DelimitIdentifier(operation.Name, operation.Schema))
                .Append(";");
        }

        protected abstract void Generate(
            [NotNull] RenameTableOperation operation,
            [CanBeNull] IModel model,
            [NotNull] SqlBatchBuilder builder);

        protected abstract void Generate(
            [NotNull] MoveTableOperation operation,
            [CanBeNull] IModel model,
            [NotNull] SqlBatchBuilder builder);

        protected virtual void Generate(
            [NotNull] AddColumnOperation operation,
            [CanBeNull] IModel model,
            [NotNull] SqlBatchBuilder builder)
        {
            Check.NotNull(operation, nameof(operation));
            Check.NotNull(builder, nameof(builder));

            builder
                .Append("ALTER TABLE ")
                .Append(_sql.DelimitIdentifier(operation.Table, operation.Schema))
                .Append(" ADD ");

            GenerateColumn(operation.Column, builder);

            builder.Append(";");
        }

        protected virtual void Generate(
            [NotNull] DropColumnOperation operation,
            [CanBeNull] IModel model,
            [NotNull] SqlBatchBuilder builder)
        {
            Check.NotNull(operation, nameof(operation));
            Check.NotNull(builder, nameof(builder));

            builder
                .Append("ALTER TABLE ")
                .Append(_sql.DelimitIdentifier(operation.Table, operation.Schema))
                .Append(" DROP COLUMN ")
                .Append(_sql.DelimitIdentifier(operation.Name))
                .Append(";");
        }

        protected virtual void Generate(
            [NotNull] AlterColumnOperation operation,
            [CanBeNull] IModel model,
            [NotNull] SqlBatchBuilder builder)
        {
            Check.NotNull(operation, nameof(operation));
            Check.NotNull(builder, nameof(builder));

            var column = operation.Column;

            builder
                .Append("ALTER TABLE ")
                .Append(_sql.DelimitIdentifier(operation.Table, operation.Schema))
                .Append(" ALTER COLUMN ");

            GenerateColumn(column, builder);

            builder.Append(";");
        }

        protected abstract void Generate(
            [NotNull] RenameColumnOperation operation,
            [CanBeNull] IModel model,
            [NotNull] SqlBatchBuilder builder);

        protected virtual void Generate(
            [NotNull] AddPrimaryKeyOperation operation,
            [CanBeNull] IModel model,
            [NotNull] SqlBatchBuilder builder)
        {
            Check.NotNull(operation, nameof(operation));
            Check.NotNull(builder, nameof(builder));

            builder
                .Append("ALTER TABLE ")
                .Append(_sql.DelimitIdentifier(operation.Table, operation.Schema))
                .Append(" ADD ");

            GeneratePrimaryKey(operation, builder);

            builder.Append(";");
        }

        protected virtual void Generate(
            [NotNull] DropPrimaryKeyOperation operation,
            [CanBeNull] IModel model,
            [NotNull] SqlBatchBuilder builder)
        {
            Check.NotNull(operation, nameof(operation));
            Check.NotNull(builder, nameof(builder));

            builder
                .Append("ALTER TABLE ")
                .Append(_sql.DelimitIdentifier(operation.Table, operation.Schema))
                .Append(" DROP CONSTRAINT ")
                .Append(_sql.DelimitIdentifier(operation.Name))
                .Append(";");
        }

        protected virtual void Generate(
            [NotNull] AddUniqueConstraintOperation operation,
            [CanBeNull] IModel model,
            [NotNull] SqlBatchBuilder builder)
        {
            Check.NotNull(operation, nameof(operation));
            Check.NotNull(builder, nameof(builder));

            builder
                .Append("ALTER TABLE ")
                .Append(_sql.DelimitIdentifier(operation.Table, operation.Schema))
                .Append(" ADD ");

            GenerateUniqueConstraint(operation, builder);

            builder.Append(";");
        }

        protected virtual void Generate(
            [NotNull] DropUniqueConstraintOperation operation,
            [CanBeNull] IModel model,
            [NotNull] SqlBatchBuilder builder)
        {
            Check.NotNull(operation, nameof(operation));
            Check.NotNull(builder, nameof(builder));

            builder
                .Append("ALTER TABLE ")
                .Append(_sql.DelimitIdentifier(operation.Table, operation.Schema))
                .Append(" DROP CONSTRAINT ")
                .Append(_sql.DelimitIdentifier(operation.Name))
                .Append(";");
        }

        protected virtual void Generate(
            [NotNull] AddForeignKeyOperation operation,
            [CanBeNull] IModel model,
            [NotNull] SqlBatchBuilder builder)
        {
            Check.NotNull(operation, nameof(operation));
            Check.NotNull(builder, nameof(builder));

            builder
                .Append("ALTER TABLE ")
                .Append(_sql.DelimitIdentifier(operation.DependentTable, operation.DependentSchema))
                .Append(" ADD ");

            GenerateForeignKey(operation, builder);

            builder.Append(";");
        }

        protected virtual void Generate(
            [NotNull] DropForeignKeyOperation operation,
            [CanBeNull] IModel model,
            [NotNull] SqlBatchBuilder builder)
        {
            Check.NotNull(operation, nameof(operation));
            Check.NotNull(builder, nameof(builder));

            builder
                .Append("ALTER TABLE ")
                .Append(_sql.DelimitIdentifier(operation.Table, operation.Schema))
                .Append(" DROP CONSTRAINT ")
                .Append(_sql.DelimitIdentifier(operation.Name))
                .Append(";");
        }

        protected virtual void Generate(
            [NotNull] CreateIndexOperation operation,
            [CanBeNull] IModel model,
            [NotNull] SqlBatchBuilder builder)
        {
            Check.NotNull(operation, nameof(operation));
            Check.NotNull(builder, nameof(builder));

            builder.Append("CREATE ");

            if (operation.Unique)
            {
                builder.Append("UNIQUE ");
            }

            GenerateIndexTraits(operation, builder);

            builder
                .Append("INDEX ")
                .Append(_sql.DelimitIdentifier(operation.Name))
                .Append(" ON ")
                .Append(_sql.DelimitIdentifier(operation.Table, operation.Schema))
                .Append(" (")
                .Append(string.Join(", ", operation.Columns.Select(_sql.DelimitIdentifier)))
                .Append(");");
        }

        protected virtual void GenerateIndexTraits(
            [NotNull] CreateIndexOperation operation,
            [NotNull] SqlBatchBuilder builder)
        {
        }

        protected virtual void Generate(
            [NotNull] DropIndexOperation operation,
            [CanBeNull] IModel model,
            [NotNull] SqlBatchBuilder builder)
        {
            Check.NotNull(operation, nameof(operation));
            Check.NotNull(builder, nameof(builder));

            builder
                .Append("DROP INDEX ")
                .Append(_sql.DelimitIdentifier(operation.Name, operation.Schema))
                .Append(";");
        }

        protected abstract void Generate(
            [NotNull] RenameIndexOperation operation,
            [CanBeNull] IModel model,
            [NotNull] SqlBatchBuilder builder);

        protected virtual void Generate(
            [NotNull] SqlOperation operation,
            [CanBeNull] IModel model,
            [NotNull] SqlBatchBuilder builder)
        {
            Check.NotNull(operation, nameof(operation));
            Check.NotNull(builder, nameof(builder));

            builder.Append(operation.Sql, operation.SuppressTransaction);
        }

        protected virtual void Generate(
            [NotNull] AlterTableOperation operation,
            [CanBeNull] IModel model,
            [NotNull] SqlBatchBuilder builder)
        {
        }

        protected virtual void GenerateColumns(
            [NotNull] IReadOnlyList<ColumnModel> columns,
            [NotNull] SqlBatchBuilder builder)
        {
            Check.NotNull(columns, nameof(columns));
            Check.NotNull(builder, nameof(builder));

            var first = true;
            foreach (var column in columns)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    builder.AppendLine(",");
                }

                GenerateColumn(column, builder);
            }
        }

        protected virtual void GenerateColumn([NotNull] ColumnModel column, [NotNull] SqlBatchBuilder builder)
        {
            Check.NotNull(column, nameof(column));
            Check.NotNull(builder, nameof(builder));

            builder
                .Append(_sql.DelimitIdentifier(column.Name))
                .Append(" ");

            builder.Append(column.StoreType);

            if (!column.Nullable)
            {
                builder.Append(" NOT NULL");
            }

            GenerateColumnTraits(column, builder);

            if (column.DefaultValueSql != null)
            {
                builder
                    .Append(" DEFAULT ")
                    .Append(column.DefaultValueSql);
            }
            else if (column.DefaultValue != null)
            {
                builder
                    .Append(" DEFAULT ")
                    .Append(_sql.GenerateLiteral((dynamic)column.DefaultValue));
            }
        }

        protected virtual void GenerateColumnTraits(
            [NotNull] ColumnModel column,
            [NotNull] SqlBatchBuilder builder)
        {
        }

        protected virtual void GeneratePrimaryKey(
            [NotNull] AddPrimaryKeyOperation operation,
            [NotNull] SqlBatchBuilder builder)
        {
            Check.NotNull(operation, nameof(operation));
            Check.NotNull(builder, nameof(builder));

            if (operation.Name != null)
            {
                builder
                    .Append("CONSTRAINT ")
                    .Append(_sql.DelimitIdentifier(operation.Name))
                    .Append(" ");
            }

            builder.Append("PRIMARY KEY");

            GeneratePrimaryKeyTraits(operation, builder);

            builder
                .Append(" (")
                .Append(string.Join(", ", operation.Columns.Select(_sql.DelimitIdentifier)))
                .Append(")");
        }

        protected virtual void GeneratePrimaryKeyTraits(
            [NotNull] AddPrimaryKeyOperation operation,
            [NotNull] SqlBatchBuilder builder)
        {
        }

        protected virtual void GenerateUniqueConstraint(
            [NotNull] AddUniqueConstraintOperation operation,
            [NotNull] SqlBatchBuilder builder)
        {
            Check.NotNull(operation, nameof(operation));
            Check.NotNull(builder, nameof(builder));

            if (operation.Name != null)
            {
                builder
                    .Append("CONSTRAINT ")
                    .Append(_sql.DelimitIdentifier(operation.Name))
                    .Append(" ");
            }

            builder.Append("UNIQUE (")
                .Append(string.Join(", ", operation.Columns.Select(_sql.DelimitIdentifier)))
                .Append(")");
        }

        protected virtual void GenerateForeignKey(
            [NotNull] AddForeignKeyOperation operation,
            [NotNull] SqlBatchBuilder builder)
        {
            Check.NotNull(operation, nameof(operation));
            Check.NotNull(builder, nameof(builder));

            if (operation.Name != null)
            {
                builder
                    .Append("CONSTRAINT ")
                    .Append(_sql.DelimitIdentifier(operation.Name))
                    .Append(" ");
            }

            builder.Append("FOREIGN KEY (")
                .Append(string.Join(", ", operation.DependentColumns.Select(_sql.DelimitIdentifier)))
                .Append(") REFERENCES ")
                .Append(_sql.DelimitIdentifier(operation.PrincipalTable, operation.PrincipalSchema));

            if (operation.PrincipalColumns.Any())
            {
                builder
                    .Append(" (")
                    .Append(string.Join(", ", operation.PrincipalColumns.Select(_sql.DelimitIdentifier)))
                    .Append(")");
            }

            if (operation.CascadeDelete)
            {
                builder.Append(" ON DELETE CASCADE");
            }
        }
    }
}
