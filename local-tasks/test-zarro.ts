/// <reference path="../node_modules/zarro/types.d.ts" />
(function () {

  const
    gulp = requireModule<Gulp>("gulp"),
    testZarro = requireModule<TestZarro>("test-zarro");

  gulp.task("test-zarro", async () => {
    await testZarro({
      packageVersion: "beta",
      rollback: true,
      tasks: ["test", "pack"]
    });
  });
})();
