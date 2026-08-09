# Configuration file for the Sphinx documentation builder.
#
# For the full list of built-in configuration values, see the documentation:
# https://www.sphinx-doc.org/en/master/usage/configuration.html

# -- Project information -----------------------------------------------------
# https://www.sphinx-doc.org/en/master/usage/configuration.html#project-information

project = 'Ocelot API Gateway'
copyright = ' 2016-2024 Three Mammals'
author = 'Tom Gardham-Pallister, Raman Maksimchuk'
release = 'v23.1 "Hornussen Sport"' # OK displayed
version = '23.1' # version is not displayed in either HTML pages or PDF docs
today = 'March 4, 2024'

# -- General configuration ---------------------------------------------------
# https://www.sphinx-doc.org/en/master/usage/configuration.html#general-configuration

extensions = [
    'sphinx_copybutton'
]

templates_path = ['_templates']
exclude_patterns = ['_build', 'Thumbs.db', '.DS_Store']

# -- Options for HTML output -------------------------------------------------
# https://www.sphinx-doc.org/en/master/usage/configuration.html#options-for-html-output

# HTML theming: https://www.sphinx-doc.org/en/master/usage/theming.html
# HTML theme development: https://www.sphinx-doc.org/en/master/development/html_themes/index.html
# https://alabaster.readthedocs.io/en/latest/
# https://www.sphinx-doc.org/en/master/usage/configuration.html#confval-html_theme
html_theme = 'alabaster'

# https://www.sphinx-doc.org/en/master/usage/configuration.html#confval-html_static_path
html_static_path = ['_static']
html_css_files = ['overrides.css']

# UNICODE characters: https://www.sphinx-doc.org/en/master/usage/configuration.html#confval-rst_epilog
rst_epilog = """
.. |ge| unicode:: U+2265 .. ≥
"""

latex_logo = '_static/ocelot_logo.png'
#   'releasename': 'Hornussen Sport',
latex_elements = {
    'preamble': r'''
    \usepackage{pifont}
    ''',
}
